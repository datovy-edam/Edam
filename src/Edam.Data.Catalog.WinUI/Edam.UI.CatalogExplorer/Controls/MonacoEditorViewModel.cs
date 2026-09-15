using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using Edam.Data.CatalogModel;
using Monaco;
using Monaco.MonacoHandler;

namespace Edam.UI.Catalog.Controls;

public class MonacoEditorViewModel : ObservableObject, IMonacoEditorModel
{
    public MonacoFileRecognitionHandler FileRecognitionHandler { get; set; } = 
        new MonacoFileRecognitionHandler();
    public const string EMPTY_DOCUMENT_NAME = "[]";

    public int ModelIndex { get; set; } = -1;
    private string _Content;
    public string Content
    {
        get { return _Content; }
        set
        {
            if (_Content != value)
            {
                _Content = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }

    private string _documentName;
    public string DocumentName
    {
        get { return _documentName; }
        set
        {
            if (_documentName != value)
            {
                _documentName = value;
                OnPropertyChanged(nameof(DocumentName));
            }
        }
    }

    private bool _isDirty;
    /// <summary>
    /// True when the editor content has changed since the last save/load.
    /// </summary>
    public bool IsDirty
    {
        get { return _isDirty; }
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    private bool _isReadOnly;
    /// <summary>
    /// True when the document should be opened in read-only mode.
    /// </summary>
    public bool IsReadOnly
    {
        get { return _isReadOnly; }
        set
        {
            if (_isReadOnly != value)
            {
                _isReadOnly = value;
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }
    }

    private string _readOnlyMessage = "This document is read-only.";
    /// <summary>
    /// Message shown when the editor is in read-only mode.
    /// </summary>
    public string ReadOnlyMessage
    {
        get { return _readOnlyMessage; }
        set
        {
            if (_readOnlyMessage != value)
            {
                _readOnlyMessage = value;
                OnPropertyChanged(nameof(ReadOnlyMessage));
            }
        }
    }

    public ObservableCollection<string> Languages { get; set; } =
        new ObservableCollection<string>();

    private string _language;
    public string CurrentLanguage
    {
        get { return _language; }
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged(nameof(CurrentLanguage));
            }
        }
    }

    private CatalogPathItem _documentPath;
    public CatalogPathItem CurrentPathItem
    {
        get => _documentPath;
        set
        {
            if (value == null)
            {
                DocumentName = EMPTY_DOCUMENT_NAME;
                CurrentLanguage = 
                    FileRecognitionHandler.RecognizeLanguageByFileType("");
            }
            else
            {
                _documentPath = value;
                DocumentName = value.Name;
                CurrentLanguage =
                    FileRecognitionHandler.
                       RecognizeLanguageByFileType(value.Extension);
                IsReadOnly = IsFileReadOnly(value.Full);
            }
        }
    }

    /// <summary>
    /// Determine whether the given path is a read-only file on disk.
    /// </summary>
    private static bool IsFileReadOnly(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);
            }
        }
        catch
        {
            // ignore — treat as editable if the path cannot be inspected
        }
        return false;
    }

    private Monaco.MonacoEditor _EditorInstance;
    public Monaco.MonacoEditor EditorInstance
    {
        get => _EditorInstance;
        set => _EditorInstance = value;
    }

    public MonacoEditorViewModel()
    {
        DocumentName = EMPTY_DOCUMENT_NAME;
        CurrentLanguage =
            FileRecognitionHandler.RecognizeLanguageByFileType("");
        //var litems = FileRecognitionHandler.Languages.Values.ToList<string>();
        //var items = from i in litems
        //            group i by i into g
        //            select g;
        //foreach (var item in litems)
        //{
        //    if ()
        //    Languages.Add(item);
        //}
    }

    public async Task<string> GetContentAsync()
    {
        return await _EditorInstance.GetEditorContentAsync();
    }
}

