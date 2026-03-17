using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace UI.Views
{
    public sealed partial class OnboardingPage : Page
    {
        private double _startX;
        private bool _isSwiping;

        public OnboardingPage()
        {
            this.InitializeComponent();
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                if (button.Name == "BtnPreviousSlide")
                {
                    PrevPage();
                }
                else if (button.Name == "BtnNextSlide")
                {
                    NextPage();
                }
            }
        }

        private void PrevPage()
        {
            if (OnboardingFlipView.SelectedIndex > 0)
            {
                OnboardingFlipView.SelectedIndex--;
            }
        }

        private void NextPage()
        {
            if (OnboardingFlipView.SelectedIndex < OnboardingFlipView.Items.Count - 1)
            {
                OnboardingFlipView.SelectedIndex++;
            }
        }

        private void OnboardingFlipView_Loaded(object sender, RoutedEventArgs e)
        {
            HideDefaultFlipViewButtons(OnboardingFlipView);
        }

        private void HideDefaultFlipViewButtons(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn &&
                   (btn.Name == "PreviousButtonHorizontal" || btn.Name == "NextButtonHorizontal"))
                {
                    btn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    HideDefaultFlipViewButtons(child);
                }
            }
        }

        private void OnboardingFlipView_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(OnboardingFlipView);

            if (pointerPoint.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                if (pointerPoint.Properties.IsLeftButtonPressed)
                {
                    _startX = pointerPoint.Position.X;
                    _isSwiping = true;
                    OnboardingFlipView.CapturePointer(e.Pointer);
                }
            }
        }

        private void OnboardingFlipView_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_isSwiping)
            {
                var currentX = e.GetCurrentPoint(OnboardingFlipView).Position.X;
                var deltaX = currentX - _startX;

                if (deltaX > 50)
                {
                    PrevPage();
                }
                else if (deltaX < -50)
                {
                    NextPage();
                }

                _isSwiping = false;
                OnboardingFlipView.ReleasePointerCapture(e.Pointer);
            }
        }

        private void OnboardingFlipView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isSwiping = false;
            OnboardingFlipView.ReleasePointerCapture(e.Pointer);
        }

        private void BtnGetStarted_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["IsFirstTime"] = false;

            this.Frame.Navigate(typeof(LoginPage));

            this.Frame.BackStack.Clear();
        }
    }
}