//    Copyright 2012 Aviad Sachs
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.


using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Threading;

namespace MobileService.Sdk.WP7
{
    /// <summary>
    /// WebAuthenticationBrokerStruct is a Class of controls for OAuth2.0 flow with Browser. (TODO: Wrap it to a full WP7 Control DLL... :)
    /// Currently used by the SDK, its purpuse is to be a standalone Component with StartUri, EndUri etc. 
    /// Its designed for WIndows Phone 7.5, but the concept is similar to Win8 Web Broker. Learn more at: http://bit.ly/VM9ek5
    /// 
    /// Basically, Your page wher you call the Broker NEEDS to have a WebBrowser and a LoadingGrid.  
    /// 
    /// WebBRowser:  Big enough for users to loging into their service,
    /// LoadingGrod: grid with a running progress bar. Broker will display and hide on demand.
    /// Dispacher:   Current page dispacher.
    /// 
    /// See an Example for the Xaml at http://bit.ly/SmwFMw
    /// Or in this code here below.
    /// </summary>
    /* 
    ///    <phone:WebBrowser Name="authorizationBrowser" Grid.RowSpan="2" IsScriptEnabled="True" Visibility="Collapsed" />
    ///
    ///    <Grid x:Name="loadingGrid" Grid.RowSpan="2" HorizontalAlignment="Stretch" Visibility="Collapsed" Background="#B9000000">
    ///        <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Center">
    ///            <TextBlock Height="30" HorizontalAlignment="Center" Name="loadingText" Text="Loading..." VerticalAlignment="Center" Width="441" TextAlignment="Center" />
    ///            <ProgressBar Height="4" HorizontalAlignment="Center" Name="loadingProgress" VerticalAlignment="Center" IsIndeterminate="True" HorizontalContentAlignment="Center" Width="400" />
    ///        </StackPanel>
    ///     </Grid>
    ///
    /// </example>
    /// 
     */ 

    public class WebAuthenticationBrokerStruct
    {
        public WebBrowser authorizationBrowser { get; set; }
        public Grid loadingGrid { get; set; }
        public Dispatcher dispacher { get; set; }

        //TODO: Seperate WebAuthenticationBroker to a different file. 
        //      Doesn't has to be part of the Azure sdk
        //
        //      [then add this delegate Action<MobileServiceUser, Exception> BrokerSuccessContinueWith();  ]
    }
}
