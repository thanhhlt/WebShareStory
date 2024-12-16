# nullable disable
namespace App.Models;

public class Post
{
    public string UserId { get; set; }
    public int PostId { get; set; }
}

public class Summernote
{
    public Summernote(string iDEditor, bool loadLibrary = true)
    {
        IDEditor = iDEditor;
        LoadLibrary = loadLibrary;
    }
    
    public Post Post { get; set; }
    public string IDEditor { get; set; }
    public bool LoadLibrary { get; set; }
    public int Height { get; set; } = 120;
    public string Toolbar { get; set; } = @"
        [
          ['style', ['style']],
          ['font', ['bold', 'italic', 'underline', 'clear', 'strikethrough', 'superscript', 'subscript']],
          ['fontsize', ['fontsize']],
          ['color', ['color']],
          ['para', ['ul', 'ol', 'paragraph']],
          ['table', ['table']],
          ['insert', ['link', 'picture', 'video']],
          ['view', ['fullscreen', 'codeview', 'help']]
        ]
    ";
}