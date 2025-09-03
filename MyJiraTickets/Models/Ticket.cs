using System.ComponentModel;

namespace MyJiraTickets.Models;

public class Ticket : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _url = string.Empty;
    private string _summary = string.Empty;
    private string _status = string.Empty;
    private string _type = "Task";
    private string _priority = "Medium";

    public string Key 
    { 
        get => _key; 
        set 
        { 
            _key = value; 
            OnPropertyChanged(nameof(Key)); 
        } 
    }
    
    public string Url 
    { 
        get => _url; 
        set 
        { 
            _url = value; 
            OnPropertyChanged(nameof(Url)); 
        } 
    }
    
    public string Summary 
    { 
        get => _summary; 
        set 
        { 
            _summary = value; 
            OnPropertyChanged(nameof(Summary)); 
        } 
    }
    
    public string Status 
    { 
        get => _status; 
        set 
        { 
            _status = value; 
            OnPropertyChanged(nameof(Status)); 
        } 
    }
    
    public string Type 
    { 
        get => _type; 
        set 
        { 
            _type = value; 
            OnPropertyChanged(nameof(Type)); 
        } 
    }
    
    public string Priority 
    { 
        get => _priority; 
        set 
        { 
            _priority = value; 
            OnPropertyChanged(nameof(Priority)); 
        } 
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}