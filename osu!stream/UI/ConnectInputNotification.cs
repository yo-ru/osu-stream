#if iOS
using System;
using UIKit;
using osum.Support.iPhone;

namespace osum.UI
{
    public class ConnectInputNotification
    {
        UIAlertController alertController;
        UITextField username;
        UITextField password;
        Action<bool, string, string> completion;

        public string Username { get; private set; }
        public string Password { get; private set; }

            public ConnectInputNotification(Action<bool, string, string> completion)
        {
            this.completion = completion;

            alertController = UIAlertController.Create("Connect", null, UIAlertControllerStyle.Alert);

            alertController.AddTextField((field) =>
            {
                field.Placeholder = "Username";
                field.AutocorrectionType = UITextAutocorrectionType.No;
                username = field;
            });
            alertController.AddTextField((field) =>
            {
                field.Placeholder = "Password";
                field.AutocorrectionType = UITextAutocorrectionType.No;
                field.SecureTextEntry = true;
                password = field;
            });

                alertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, action => HandleAction(false)));
            alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, action => HandleAction(true)));

            AppDelegate.SetUsingViewController(true);
            AppDelegate.ViewController.PresentViewController(alertController, true, null);
        }

        void HandleAction(bool isOk)
        {
            if (isOk)
            {
                Username = username.Text;
                Password = password.Text;
            }
            else
            {
                Username = null;
                Password = null;
            }

            completion(isOk, Username, Password);
            alertController.Dispose();
            AppDelegate.SetUsingViewController(false);
        }
    }
}
#endif