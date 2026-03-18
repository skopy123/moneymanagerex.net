using System.Diagnostics;
using mmex.net.core.Entities;
using mmex.net.core.Services;

namespace mmex.net.winform.Panels;

/// <summary>
/// Reusable panel for managing file attachments on any entity.
/// Works in buffered mode: changes are only written to disk and DB when
/// <see cref="CommitAsync"/> is called (typically when the parent dialog is accepted).
/// </summary>
public class AttachmentPanel : UserControl
{
    private readonly IAttachmentService _service;
    private readonly string _attachmentFolder;

    private readonly ListView _list;
    private readonly Button _btnAdd;
    private readonly Button _btnRemove;
    private readonly Button _btnOpen;

    // Buffered changes
    private readonly List<(string SourcePath, string? Description)> _pendingAdds = new();
    private readonly List<long> _pendingDeletes = new();

    // Autocomplete suggestions loaded from DB
    private string[] _descriptionSuggestions = [];

    public AttachmentPanel(IAttachmentService service, string attachmentFolder)
    {
        _service = service;
        _attachmentFolder = attachmentFolder;

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _list.Columns.Add("File", 200);
        _list.Columns.Add("Description", -2);
        _list.SelectedIndexChanged += (_, _) => UpdateButtons();
        _list.DoubleClick += OnOpenClick;
        _list.AllowDrop = true;
        _list.DragEnter += (_, e) =>
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy : DragDropEffects.None;
        _list.DragDrop += OnDragDrop;

        _btnAdd    = new Button { Text = "Add...", Width = 80 };
        _btnRemove = new Button { Text = "Remove", Width = 80, Enabled = false };
        _btnOpen   = new Button { Text = "Open",   Width = 80, Enabled = false };

        _btnAdd.Click    += OnAddClick;
        _btnRemove.Click += OnRemoveClick;
        _btnOpen.Click   += OnOpenClick;

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            FlowDirection = FlowDirection.LeftToRight
        };
        btnPanel.Controls.AddRange(new Control[] { _btnAdd, _btnRemove, _btnOpen });

        Controls.Add(_list);
        Controls.Add(btnPanel);
    }

    /// <summary>Loads existing attachments for an already-persisted entity.</summary>
    public async Task LoadAsync(string refType, long refId)
    {
        _descriptionSuggestions = await _service.GetAllDescriptionsAsync();
        _list.Items.Clear();
        var attachments = await _service.GetByRefAsync(refType, refId);
        foreach (var att in attachments)
            _list.Items.Add(MakeItem(att.FileName, att.Description, att));
    }

    /// <summary>
    /// Writes all buffered adds/removes to disk and DB.
    /// Safe to call for new records (refId not known until after parent is saved).
    /// </summary>
    public async Task CommitAsync(string refType, long refId)
    {
        foreach (var id in _pendingDeletes)
            await _service.DeleteAsync(id, _attachmentFolder);
        _pendingDeletes.Clear();

        foreach (var (sourcePath, description) in _pendingAdds)
        {
            var att = new Attachment
            {
                RefType = refType,
                RefId = refId,
                Description = description
            };
            await _service.AddAsync(att, sourcePath, _attachmentFolder);
        }
        _pendingAdds.Clear();
    }

    // -----------------------------------------------------------------------

    private void OnAddClick(object? sender, EventArgs e)
    {
        using var fileDlg = new OpenFileDialog
        {
            Title = "Select file to attach",
            Filter = "All files (*.*)|*.*"
        };
        if (fileDlg.ShowDialog(this) != DialogResult.OK) return;
        AddFile(fileDlg.FileName);
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;
        // Bring the parent window to front so the prompt appears above the drag source
        (TopLevelControl as Form)?.Activate();
        foreach (var file in files)
            AddFile(file);
    }

    private void AddFile(string sourcePath)
    {
        string? description = null;
        using var descDlg = new DescriptionPrompt(_descriptionSuggestions);
        if (descDlg.ShowDialog(this) == DialogResult.OK)
            description = descDlg.Value;

        _pendingAdds.Add((sourcePath, description));
        _list.Items.Add(MakeItem(Path.GetFileName(sourcePath), description, tag: null));
    }

    private void OnRemoveClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0) return;
        var item = _list.SelectedItems[0];

        if (item.Tag is Attachment att)
        {
            _pendingDeletes.Add(att.Id);
        }
        else
        {
            // Pending add — remove from the buffer by matching filename
            var name = item.Text;
            var idx = _pendingAdds.FindLastIndex(p => Path.GetFileName(p.SourcePath) == name);
            if (idx >= 0) _pendingAdds.RemoveAt(idx);
        }

        _list.Items.Remove(item);
        UpdateButtons();
    }

    private void OnOpenClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0) return;
        var item = _list.SelectedItems[0];

        string? filePath;
        if (item.Tag is Attachment att)
        {
            filePath = Path.Combine(_attachmentFolder, att.RefType, att.FileName);
        }
        else
        {
            var name = item.Text;
            var pending = _pendingAdds.FindLast(p => Path.GetFileName(p.SourcePath) == name);
            filePath = pending.SourcePath;
        }
        

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        else
            MessageBox.Show("File not found.", "Open Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void UpdateButtons()
    {
        var has = _list.SelectedItems.Count > 0;
        _btnRemove.Enabled = has;
        _btnOpen.Enabled = has;
    }

    private static ListViewItem MakeItem(string fileName, string? description, object? tag)
    {
        var item = new ListViewItem(fileName);
        item.SubItems.Add(description ?? string.Empty);
        item.Tag = tag;
        return item;
    }
}

// ---------------------------------------------------------------------------
// Tiny description-prompt dialog — file-scoped, not exposed outside this file.
// ---------------------------------------------------------------------------

file sealed class DescriptionPrompt : Form
{
    private readonly TextBox _txt;
    public string? Value => string.IsNullOrWhiteSpace(_txt.Text) ? null : _txt.Text.Trim();

    public DescriptionPrompt(string[] suggestions)
    {
        Text = "Description (optional)";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 130);

        _txt = new TextBox { Dock = DockStyle.Top, Margin = new Padding(8) };
        if (suggestions.Length > 0)
        {
            _txt.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _txt.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var source = new AutoCompleteStringCollection();
            source.AddRange(suggestions);
            _txt.AutoCompleteCustomSource = source;
        }

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            FlowDirection = FlowDirection.RightToLeft
        };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 75 };
        var btnOk     = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Width = 75 };
        btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });

        var lbl = new Label { Text = "Enter a description for this attachment:", Dock = DockStyle.Top };

        Controls.Add(_txt);
        Controls.Add(btnPanel);
        Controls.Add(lbl);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
