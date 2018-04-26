﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers.CameraHelper;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// Camera Control to preview video. Can subscribe to video frames, software bitmap when they arrive.
    /// </summary>
    public sealed class CameraPreview : Control, IDisposable
    {
        private CameraHelper _cameraHelper;
        private MediaPlayer _mediaPlayer;
        private MediaPlayerElement _mediaPlayerElementControl;
        private Button _toggleFrameSourceGroup;
        private int _selectedSourceIndex = 0;

        /// <summary>
        /// Gets Frame Source Groups available for Camera Media Capture.
        /// </summary>
        public IReadOnlyList<MediaFrameSourceGroup> FrameSourceGroups { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraPreview"/> class.
        /// </summary>
        public CameraPreview()
        {
            this.DefaultStyleKey = typeof(CameraPreview);
        }

        /// <summary>
        /// Event raised when a new frame arrives.
        /// </summary>
        public event EventHandler<FrameEventArgs> FrameArrived;

        /// <summary>
        /// Gets or sets icon for Frame Source Group Button
        /// </summary>
        public ImageSource FrameSourceGroupButtonIcon
        {
            get { return (ImageSource)GetValue(FrameSourceGroupButtonIconProperty); }
            set { SetValue(FrameSourceGroupButtonIconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrameSourceGroupButtonIcon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrameSourceGroupButtonIconProperty =
            DependencyProperty.Register("FrameSourceGroupButtonIcon", typeof(ImageSource), typeof(CameraPreview), new PropertyMetadata(null));

        protected async override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_toggleFrameSourceGroup != null)
            {
                _toggleFrameSourceGroup.Click -= ToggleFrameSourceGroup_ClickAsync;
            }

            _mediaPlayerElementControl = (MediaPlayerElement)GetTemplateChild("MediaPlayerElementControl");
            _toggleFrameSourceGroup = (Button)GetTemplateChild("FrameSourceGroupButton");

            if (_toggleFrameSourceGroup != null)
            {
                _toggleFrameSourceGroup.Click += ToggleFrameSourceGroup_ClickAsync;
            }

            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (_cameraHelper == null)
            {
                _cameraHelper = new CameraHelper();
                var result = await _cameraHelper.InitializeAndStartCaptureAsync();
                if (result == CameraHelperResult.Success)
                {
                    // Subscribe to the frames as they arrive
                    _cameraHelper.FrameArrived += CameraHelper_FrameArrived;
                    FrameSourceGroups = _cameraHelper.FrameSourceGroups;
                }

                SetUIControls(result);
            }
        }

        private void SetMediaPlayerSource()
        {
            var frameSource = _cameraHelper?.FrameSource;
            if (frameSource != null)
            {
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new MediaPlayer
                    {
                        AutoPlay = true,
                        RealTimePlayback = true
                    };
                }

                _mediaPlayer.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
                _mediaPlayerElementControl.SetMediaPlayer(_mediaPlayer);
            }
        }

        private void CameraHelper_FrameArrived(object sender, FrameEventArgs e)
        {
            EventHandler<FrameEventArgs> handler = FrameArrived;
            handler?.Invoke(sender, e);
        }

        private async void ToggleFrameSourceGroup_ClickAsync(object sender, RoutedEventArgs e)
        {
            _selectedSourceIndex = _selectedSourceIndex < FrameSourceGroups.Count ? ++_selectedSourceIndex : 0;
            var group = FrameSourceGroups[_selectedSourceIndex];
            var result = await _cameraHelper.InitializeAndStartCaptureAsync(group);
            SetUIControls(result);
        }

        private void SetUIControls(CameraHelperResult result)
        {
            var success = result == CameraHelperResult.Success;
            if (success)
            {
                SetMediaPlayerSource();
            }
            else
            {
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
                _mediaPlayerElementControl.SetMediaPlayer(null);
            }

            _toggleFrameSourceGroup.IsEnabled = success;
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            if (_cameraHelper != null)
            {
                _cameraHelper.FrameArrived -= CameraHelper_FrameArrived;
                _cameraHelper.Dispose();
                _cameraHelper = null;
            }

            if (_mediaPlayer != null)
            {
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }
        }
    }
}