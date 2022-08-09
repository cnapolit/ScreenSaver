using ScreenSaver.Models.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

//TODO: Properly investigate way to make generic EnumConverter<t> work with wpf/xaml
// Possible solution may involve factory: https://stackoverflow.com/questions/8235421/how-do-i-set-wpf-xaml-forms-design-datacontext-to-class-that-uses-generic-type/8235459#8235459
namespace ScreenSaver.Services.UI
{
    public class AudioSourceConverter : BaseValueConverter<AudioSourceConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value;

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (AudioSource)value;
    }
    public class PlayStateConverter : BaseValueConverter<PlayStateConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value;

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (PlayState)value;
    }

    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)((TimeSpan)value).TotalSeconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.FromSeconds((int)value);
        }
    }

    public abstract class BaseValueConverter<T> : MarkupExtension, IValueConverter where T : class, new()
    {
        private static T _converter;

        public override object ProvideValue(IServiceProvider serviceProvider) => _converter ?? (_converter = new T());

        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
    }
}