#if ANDROID
using System;
using Android.App;
using Android.Views;
using Android.Widget;

namespace osum.UI
{
    public class ConnectInputNotification
    {
        AlertDialog alertDialog;
        EditText username;
        EditText password;
        Action<bool, string, string> completion;

        public string Username { get; private set; }
        public string Password { get; private set; }

        public ConnectInputNotification(Activity activity, Action<bool, string, string> completion)
        {
            this.completion = completion;

            var builder = new AlertDialog.Builder(activity);
            builder.SetTitle("Connect");

            var view = activity.LayoutInflater.Inflate(Resource.Layout.dialog_connect, null);

            username = view.FindViewById<EditText>(Resource.Id.username);
            password = view.FindViewById<EditText>(Resource.Id.password);

            builder.SetView(view);

            builder.SetNegativeButton("Cancel", (s, e) => HandleAction(false));
            builder.SetPositiveButton("OK", (s, e) => HandleAction(true));

            alertDialog = builder.Create();
            alertDialog.Show();
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

            completion?.Invoke(isOk, Username, Password);
            alertDialog.Dismiss();
        }
    }
}
#endif
