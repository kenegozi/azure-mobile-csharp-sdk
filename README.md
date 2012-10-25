azure-mobile-wp7-sdk
=======================

This is all still very Experimental.

This is a C# (CSharp) SDK for Azure Mobile Services for Windows Phone 7.5.

In your entry class, or anywhere else visible (eg App.xaml.cs, Main() etc.):

    public partial class App : Application
    {
        public static readonly MobileServiceClient MobileServiceClient;
        public static User CurrentUser;
    
        static App()
        {
            // Get this data from the management portal's quickstart page
            // in the 'connect to existing apps' section
            MobileServiceClient = new MobileServiceClient(
                "https://YOUR_APP.azure-mobile.net/",
                "YOUR_APP_KEY"
            );
        }
        
        // the rest of App.xaml.cs here ...
    }
  
Grab a table reference (typed - you can use the non-generic method and get a Table that works with JObject):

    MobileServiceTable<TodoItem> todoItemTable = App.MobileServiceClient.GetTable<TodoItem>();
    
Insert:

    var item = new TodoItem { Text = "Do this!" };
    todoItemTable.Insert(item, (res, err) => {
        if (err != null) {
            //handle it
            return;
        }
        item = res;
    });
  
  
Update:

    var updates = new JObject();
    updates["text"] = "The text";
    todoItemTable.Update(updates, err => {
        if (err != null) {
            //handle it
        }
    });
    
Get all:

    todoItemTable.GetAll((res, err) => {
        if (err != null) {
            //handle it
            return;
        }
        foreach (TodoItem in res) {
        }
    });
    
OData query:

    var query = new MobileServiceQuery()
        .Filter("text eq 'whatever'")
        .Top(1)
        .Skip(2)
        .OrderBy("id desc");
    
    todoItemTable.Get(query, (res, err) => {
        if (err != null) {
            //handle it
            return;
        }
        foreach (TodoItem in res) {
        }
    });
  
Delete:

    testStuffTable.Delete(item.Id, err => {
        if (err != null) {
            //handle it
        }
    });

Login to Azure Mobile Services with the AuthenticationToken returned  from LiveAuthClient or the LiveLoginButton's SessionChanged event:

    App.MobileServiceClient.Login(e.Session.AuthenticationToken, (userId, err) => {
       // do something with userId, perhaps call to LiveConnect for user details, etc.
    });

Use, abuse, and report back !
