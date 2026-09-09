using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project2FA.Core;
using Project2FA.Repository.Models;
using Project2FA.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using WebDAVClient;
using System.Threading;
using System.Threading.Tasks;
using UNOversal.Ioc;
using UNOversal.Services.Dialogs;
using UNOversal.Services.Secrets;
using UNOversal.Services.Serialization;
using DryIoc.FastExpressionCompiler.LightExpression;


#if WINDOWS_UWP
using Windows.UI.Xaml;
using Project2FA.UWP;
#else
using Project2FA.UnoApp;
#endif

namespace Project2FA.ViewModels
{
    public partial class WebDAVAuthContentDialogViewModel : ObservableObject, IDialogInitialize
    {
        // ── observable fields ───────────────────────────────────────────────────
        //[ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(IsFlowV2))]
        //[NotifyPropertyChangedFor(nameof(IsFlowV2Inverse))]
        //[NotifyPropertyChangedFor(nameof(IsPolling))]
        //[NotifyPropertyChangedFor(nameof(IsPollingInverse))]
        //[NotifyPropertyChangedFor(nameof(IsNotPolling))]


        private bool _useEmbeddedWebView;
        public bool UseEmbeddedWebView
        {
            get => _useEmbeddedWebView;
            set
            {
                if(SetProperty(ref _useEmbeddedWebView, value))
                {
                    OnUseEmbeddedWebViewChanged(value);
                }
            }
        }

        private bool _isPollingEnabled;
        public bool IsPollingFlag
        {
            get => _isPollingEnabled;
            set => SetProperty(ref _isPollingEnabled, value);
        }

        public bool IsPollingInverseFlag
        {
            get => !_isPollingEnabled;
            set => SetProperty(ref _isPollingEnabled, !value);
        }

        private bool _isSuccessFlag;

        public bool IsSuccessFlag
        {
            get => _isSuccessFlag;
            set => SetProperty(ref _isSuccessFlag, value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string _serverAddress;
        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        private string _loginUrl;
        public string LoginUrl 
        {
            get => _loginUrl;
            set
            {
                if(SetProperty(ref _loginUrl, value))
                {
                    OnServerAddressChanged(value);
                }
            }
        }

        // v1 WebView2 source
        private Uri _url;
        public Uri URL
        {
            get => _url;
            private set => SetProperty(ref _url, value);
        }

        // cancellation for the poll loop
        private CancellationTokenSource _pollCts;

        // ── Visibility-typed computed properties (used by x:Bind in XAML) ──────
        public Visibility IsFlowV2 => UseEmbeddedWebView ? Visibility.Collapsed : Visibility.Visible;
        public Visibility IsFlowV2Inverse => UseEmbeddedWebView ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPolling => IsPollingFlag ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPollingInverse => IsPollingFlag ? Visibility.Collapsed : Visibility.Visible;
        public Visibility IsSuccess => IsSuccessFlag ? Visibility.Visible : Visibility.Collapsed;

        // ── bool inverse of IsPollingFlag (safe for IsEnabled bindings) ─────────
        public bool IsNotPolling => !IsPollingFlag;

        // ── init ────────────────────────────────────────────────────────────────
        public void Initialize(IDialogParameters parameters)
        {
            if (parameters.TryGetValue<string>("serverAddress", out var addr))
            {
                ServerAddress = addr.TrimEnd('/');
            }
            UseEmbeddedWebView = false; // v2 is the default
        }

        // ── react to mode changes ───────────────────────────────────────────────
        private void OnUseEmbeddedWebViewChanged(bool value)
        {
            if (value && !string.IsNullOrWhiteSpace(ServerAddress))
            {
                SetEmbeddedLoginUrl(ServerAddress);
            }
        }

        private void OnServerAddressChanged(string value)
        {
            if (UseEmbeddedWebView && !string.IsNullOrWhiteSpace(value))
            {
                SetEmbeddedLoginUrl(ServerAddress);
            }
        }

        private void SetEmbeddedLoginUrl(string address)
        {
            try { URL = WebDavTransport.RequireHttps(address.TrimEnd('/') + "/index.php/login/flow"); }
            catch (ArgumentException)
            {
                URL = null;
                IsError = true;
                ErrorMessage = "Enter an HTTPS WebDAV server address without embedded credentials.";
            }
        }

        // ── Login Flow v2: initiate ─────────────────────────────────────────────
        [RelayCommand]
        public async Task StartLoginFlowV2Async()
        {
            IsError = false;
            ErrorMessage = null;
            IsPollingFlag = false;
            IsSuccessFlag = false;
            OnPropertyChanged(nameof(IsPolling));
            OnPropertyChanged(nameof(IsPollingInverse));
            OnPropertyChanged(nameof(IsSuccess));

            if (string.IsNullOrWhiteSpace(ServerAddress))
            {
                IsError = true;
                ErrorMessage = Strings.Resources.WebDAVAuthServerAddressEmpty;
                return;
            }

            Uri origin;
            try { origin = WebDavTransport.RequireHttps(ServerAddress); }
            catch (ArgumentException)
            {
                IsError = true;
                ErrorMessage = "Enter an HTTPS WebDAV server address without embedded credentials.";
                return;
            }
            var initUrl = origin.AbsoluteUri.TrimEnd('/') + "/index.php/login/v2";

            NextcloudLoginFlowV2Response flowResponse;
            try
            {
                using var http = WebDavTransport.CreateLoginClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("2fast/1.0");
                var httpResponse = await http.PostAsync(initUrl, content: null);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    IsError = true;
                    ErrorMessage = string.Format(
                        Strings.Resources.WebDAVAuthFlowV2InitFailed,
                        (int)httpResponse.StatusCode);
                    return;
                }

                var json = await httpResponse.Content.ReadAsStringAsync();
                var serializer = App.Current.Container.Resolve<ISerializationService>();
                flowResponse = serializer.Deserialize<NextcloudLoginFlowV2Response>(json);
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = "Unable to complete secure WebDAV login. Check the HTTPS server address and try again.";
                return;
            }

            if (flowResponse?.Poll == null || string.IsNullOrEmpty(flowResponse.Login))
            {
                IsError = true;
                ErrorMessage = Strings.Resources.WebDAVAuthFlowV2InvalidResponse;
                return;
            }

            try
            {
                WebDavTransport.RequireSameOrigin(flowResponse.Login, origin);
                WebDavTransport.RequireSameOrigin(flowResponse.Poll.Endpoint, origin);
            }
            catch (ArgumentException)
            {
                IsError = true;
                ErrorMessage = "The login response contains an unsafe endpoint. Use the configured HTTPS server.";
                return;
            }
            LoginUrl = flowResponse.Login;
            IsPollingFlag = true;
            OnPropertyChanged(nameof(IsPolling));
            OnPropertyChanged(nameof(IsPollingInverse));

            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            await PollForCredentialsAsync(flowResponse.Poll.Endpoint, flowResponse.Poll.Token, origin, _pollCts.Token);
        }

        private async Task PollForCredentialsAsync(string pollEndpoint, string token, Uri origin, CancellationToken ct)
        {
            var serializer = App.Current.Container.Resolve<ISerializationService>();
            var secretService = App.Current.Container.Resolve<ISecretService>();

            using var http = WebDavTransport.CreateLoginClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("2fast/1.0");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, ct);

                    var reqBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("token", token)
                    });
                    var resp = await http.PostAsync(WebDavTransport.RequireSameOrigin(pollEndpoint, origin), reqBody, ct);

                    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // User has not yet completed the login – keep polling
                        continue;
                    }

                    if (resp.IsSuccessStatusCode)
                    {
                        var json = await resp.Content.ReadAsStringAsync();
                        var creds = serializer.Deserialize<NextcloudLoginFlowV2Credentials>(json);
                        if (creds != null && !string.IsNullOrEmpty(creds.AppPassword))
                        {
                            // Persist credentials in the same slots used by CheckLoginAsync
                            var server = WebDavTransport.RequireSameOrigin(string.IsNullOrEmpty(creds.Server) ? origin.AbsoluteUri : creds.Server, origin).AbsoluteUri;
                            secretService.Helper.WriteSecret(Constants.ContainerName, "WDPassword", creds.AppPassword);
                            secretService.Helper.WriteSecret(Constants.ContainerName, "WDUsername", creds.LoginName);
                            secretService.Helper.WriteSecret(Constants.ContainerName, "WDServerAddress", server);
                            // Keep ServerAddress in sync so callers can read it back
                            ServerAddress = server;
                            IsPollingFlag = false;
                            IsSuccessFlag = true;
                            OnPropertyChanged(nameof(IsPolling));
                            OnPropertyChanged(nameof(IsPollingInverse));
                            OnPropertyChanged(nameof(IsSuccess));
                            return;
                        }
                    }

                    IsPollingFlag = false;
                    OnPropertyChanged(nameof(IsPolling));
                    OnPropertyChanged(nameof(IsPollingInverse));
                    IsError = true;
                    ErrorMessage = string.Format(
                        Strings.Resources.WebDAVAuthFlowV2PollFailed,
                        (int)resp.StatusCode);
                    return;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    IsPollingFlag = false;
                    OnPropertyChanged(nameof(IsPolling));
                    OnPropertyChanged(nameof(IsPollingInverse));
                    IsError = true;
                    ErrorMessage = "Unable to complete secure WebDAV login. Check the HTTPS server address and try again.";
                    return;
                }
            }

            IsPollingFlag = false;
            OnPropertyChanged(nameof(IsPolling));
            OnPropertyChanged(nameof(IsPollingInverse));
            if (!IsSuccessFlag)
            {
                IsError = true;
                ErrorMessage = Strings.Resources.WebDAVAuthFlowV2Timeout;
            }
        }

        public void CancelPolling()
        {
            _pollCts?.Cancel();
            IsPollingFlag = false;
            OnPropertyChanged(nameof(IsPolling));
            OnPropertyChanged(nameof(IsPollingInverse));
        }
    }
}