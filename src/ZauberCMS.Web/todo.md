Background
We need to refactor keeping the SOLID principles in place. you are a principle architect with epxert knowledge in c# and the best practies when it comes to design , architecture, SOLID and the c# language.  We need to abstract out some features in the ZauberCMS* projects.   We have integrated a CMS that made use of the standard asp.net identity feature.

we have ported identity over to use ravendb (src/Electra.Persistence.RavenDB/Identity) and we need to now have the ZauberCMS libraries make use of it.  However, first we need to fix the design of the CmsUser and the CmsRole classes in (src/ZauberCMS.Core/Membership/Models).  We need to abstract out the following code in CmsUser:

```
    //public Media? ProfileImage { get; set; }
    //public Guid? ProfileImageId { get; set; }
    /// <summary>
    /// The content properties
    /// </summary>n
    public List<UserPropertyValue> PropertyData { get; set; } = [];
    public List<UserRole> UserRoles { get; set; } = [];
    private Dictionary<string, string>? _contentValues;
    public Dictionary<string, object> ExtendedData { get; set; } = new();
    /// <summary>
    /// If parent ids are set this could have children
    /// </summary>
    [JsonIgnore]
    public List<Audit.Models.Audit> Audits { get; set; } = [];
    public string? Name
    {
        get => this.UserName;
        set => this.UserName = value;
    }
    
    public Dictionary<string, string> ContentValues()
        => _contentValues ??= PropertyData.ToDictionary(x => x.Alias, x => x.Value);
```

Implement this in the best way possible utilizing seperation of concerns and the SRP. I'm thinking it those properties should live in their own seperate class. that has a userid as a ref so tht ravendb can call Include(x => x.Id) to load the related document/entity.  

CmsRole located in src/ZauberCMS.Core/Membership/Models needs to be refactored as well. It inherits from a ITreeItem which is for user interface purposes.  This should be refactored to be a seperate class whose primary purpose is to deal with UI stuff. Roles should just have info on the role.  

Refactor these to keep ravendb as a first class data store and also  keep the UI seperate (think DDD) and have the Identity bits stil work and function properly.  
