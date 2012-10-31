using System;
using System.Windows;

namespace easySync
{
    public static class ErrorHelper
    {
        public static class Errors
        {
        }

        public static void ShowErrorBox(String message = null, Exception e = null, bool terminateApplication = false)
        {
            String msg = String.IsNullOrEmpty(message) ? String.Empty : message;
            String exMsg = (e == null) ? String.Empty : describeException(e);
            if (message != null)
            {
                Log.Write(message);
            }
            if (e != null)
            {
                Log.Write(e, 2);
            }

            MessageBox.Show(String.Format(Properties.Resources.ErrorFormat, msg, exMsg), Properties.Resources.ErrorTitle, 
                MessageBoxButton.OK, MessageBoxImage.Error);

            if (terminateApplication)
            {
                Environment.FailFast(message, e);
            }
        }

        private static String describeException(Exception e)
        {
            return String.Format(Consts.Log.ExceptionFormat, e.GetType().Name, e.Message,
                e.TargetSite.ReflectedType.FullName, e.TargetSite.Name);
        }
    }

}
