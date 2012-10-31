using System.Windows;
using System.Windows.Controls;


namespace easySync.CustomControls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:easySync.CustomControls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:easySync.CustomControls;assembly=easySync.CustomControls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:RichButtonControl/>
    ///
    /// </summary>
    public class ImageButton : Button
    {
        #region Fields

        private const string PROPERTY_IMAGEPATH = "ImagePath";
        private const string PROPERTY_TEXT = "Text";


        // Dependency property backing variables
        public static readonly DependencyProperty ImagePathProperty;
        public static readonly DependencyProperty TextProperty;

        #endregion

        #region Constructors

        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton)));

            // Initialize ImagePath dependency properties
            ImagePathProperty = DependencyProperty.Register(PROPERTY_IMAGEPATH, typeof(string), typeof(ImageButton), new UIPropertyMetadata(null));
            TextProperty = DependencyProperty.Register(PROPERTY_TEXT, typeof(string), typeof(ImageButton), new UIPropertyMetadata(null));
        }
        
        public ImageButton()
        {
            this.Focusable = false;
        }

        #endregion

        #region Dependency Property Wrappers

        /// <summary>
        /// The ImagePath dependency property.
        /// </summary>
        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        /// <summary>
        /// The Text dependency property.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #endregion
    }
}
