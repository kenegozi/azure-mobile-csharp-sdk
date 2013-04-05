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

namespace MobileServices.Sdk
{
    /// <summary>
    /// An authenticated Mobile Services user.
    /// </summary>
    public class MobileServiceUser
    {
        /// <summary>
        /// Initializes a new instance of the MobileServiceUser class.
        /// </summary>
        /// <param name="userId">
        /// User ID of a successfull authenticated user.
        /// </param>
        internal MobileServiceUser(string userId)
        {
            this.UserId = userId;
        }

        /// <summary>
        /// Gets the user ID of a successfully authenticated user.
        /// </summary>
        public string UserId { get; private set; }
    }
}
