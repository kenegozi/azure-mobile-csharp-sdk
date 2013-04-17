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

 

Login to Azure Mobile Services:

Multi-auth Login is now supported ! (Microsoft Account, Facebook, Twitter & Google)
This can be done in two ways: 
 
1) Using LiveSDK for Microsoft accounts, and get the User Token in no time, 
   OR AFTER the first time with step (2), and a saved Token, just use LoginInBackground() :

            App.MobileServiceClient.LoginInBackground( AuthenticationToken, (userId, err) => {
               // do something with userId ... 
            });

2) For the first time, we will need a web access (OAuth2.0) to get autherizationToken from 
   the Providers. Implement a WebBrowser and a simple Grid with an Indeterminate ProgressBar running,
   and use the WebAuthenticationBroker we built for WP7. for example, add this to your XAML:

                <phone:WebBrowser Name="authorizationBrowser" Grid.RowSpan="2" IsScriptEnabled="True" Visibility="Collapsed" />

                <Grid x:Name="loadingGrid" Grid.RowSpan="2" HorizontalAlignment="Stretch" Visibility="Collapsed" Background="#B9000000">
                    <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Center">
                        <TextBlock Height="30" HorizontalAlignment="Center" Name="loadingText" Text="Loading..." VerticalAlignment="Center" Width="441" TextAlignment="Center" />
                        <ProgressBar Height="4" HorizontalAlignment="Center" Name="loadingProgress" VerticalAlignment="Center" IsIndeterminate="True" HorizontalContentAlignment="Center" Width="400" />
                    </StackPanel>
                </Grid>

then use LoginWithBrowser like this:

            var provider = MobileServiceAuthenticationProvider.Facebook;
            var broker = new WebAuthenticationBrokerStruct() { 
                    authorizationBrowser = this.authorizationBrowser, 
                    loadingGrid = this.loadingGrid, 
                    dispacher = this.Dispatcher 
            };
            
            App.MobileServiceClient.LoginWithBrowser(provider, broker, (userId, err) =>        
                // do something with userId ... 
            });

Good Luck !

Use, abuse, and report back !
