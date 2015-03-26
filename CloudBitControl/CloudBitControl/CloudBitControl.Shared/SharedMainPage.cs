using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Net.Http;

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

using LitttleBitsManager;

namespace CloudBitControl
{
    public sealed partial class MainPage : Page
    {

        string id = "00e04c1ef00d";
        string accessToken = "fac34b169ee0eafda9d0815611c4f2f6976ed12454177ac8e2f65afe56b4be01";

        LittleBitsCloudBit cloudBit = null;

        private async void readButton_Click(object sender, RoutedEventArgs e)
        {
            Task<CloudBitReadResult> readTask = cloudBit.ReadSetting();

            CloudBitReadResult result = await readTask;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    switch (result.Status)
                    {
                        case CloudBitReadResultStatus.HTTTP_ERROR:
                        case CloudBitReadResultStatus.REQUEST_TIMEOUT:
                            readTextBlock.Text = result.Status.ToString();
                            break;

                        case CloudBitReadResultStatus.READ_OK:
                            readTextBlock.Text = result.CloudBitInputValue.ToString();
                            break; 
                    }
                });

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (cloudBit == null)
            {
                cloudBit = new LittleBitsCloudBit(id, accessToken);
            }
            base.OnNavigatedTo(e);
        }

        private async void writeButton_Click(object sender, RoutedEventArgs e)
        {
            int sendValue = (int) Math.Round(valueSlider.Value);

            CloudBitWriteResult result = await cloudBit.SendSetting(sendValue, 5000);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    writeStatusTextBlock.Text = result.ToString();
                });
        }

        private async void checkStatusButton_Click(object sender, RoutedEventArgs e)
        {
            CloudBitStatus result = await cloudBit.ReadStatus();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                checkStatusTextBlock.Text = result.ToString();
            });
        }

    }
}
