﻿using CommunityToolkit.Core.Extensions.Workarounds;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Core.Platform;

public class PopupRenderer : Flyout
{
	const double defaultBorderThickness = 2;
	const double defaultSize = 600;
	readonly IMauiContext mauiContext;

	internal XamlStyle FlyoutStyle { get; set; } = new (typeof(FlyoutPresenter));
	internal WrapperControl? Control { get; set; }
	public IBasePopup? VirtualView { get; private set; }

	public PopupRenderer(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext;
	}

	public void SetElement(IBasePopup element)
	{
		VirtualView = element;

		CreateControl();
		ConfigureControl();
	}

	void CreateControl()
	{
		if (Control is null && VirtualView?.Content is not null)
		{
			Control = new WrapperControl((View)VirtualView.Content, mauiContext);
			Content = Control;
		}

		SetEvents();
	}

	public void ConfigureControl()
	{
		SetFlyoutColor();
		SetSize();
		ApplyStyles();
	}

	void SetEvents()
	{
		Closing += OnClosing;
	}

	void SetSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		_ = Control ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = VirtualView.Size != default ? VirtualView.Size : standardSize;
		Control.Width = currentSize.Width;
		Control.Height = currentSize.Height;

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
	}

	void SetLayout()
	{
		LightDismissOverlayMode = LightDismissOverlayMode.On;
		if (VirtualView is not null)
		{
			this.SetDialogPosition(VirtualView.VerticalOptions, VirtualView.HorizontalOptions);
		}
	}

	internal void ResetStyle()
	{
		FlyoutStyle = new(typeof(FlyoutPresenter));
	}

	public void SetBorderColor(Color? borderColor = null)
	{
		ResetStyle();
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.PaddingProperty, 0));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderThicknessProperty, new UWPThickness(defaultBorderThickness)));

		if (VirtualView is null)
		{
			return;
		}

		if (borderColor is null)
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, Color.FromHex("#2e6da0").ToNative()));
		}
		else
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, borderColor.ToNative()));
		}
		ConfigureControl();
	}

	void SetFlyoutColor()
	{
		_ = VirtualView?.Content ?? throw new NullReferenceException(nameof(IBasePopup.Content));

		var color = VirtualView.Color ?? Colors.Transparent;

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BackgroundProperty, color.ToWindowsColor()));

		if (VirtualView.Color == Colors.Transparent)
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, false));
		}
	}

	public void ApplyStyles()
	{
		ArgumentNullException.ThrowIfNull(Control);
		FlyoutPresenterStyle = FlyoutStyle;
	}

	public void Show()
	{
		if (VirtualView is null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(VirtualView.Parent);

		if (VirtualView.Anchor is not null)
		{
			var anchor = VirtualView.Anchor.ToNative(mauiContext);
			SetAttachedFlyout(anchor, this);
			ShowAttachedFlyout(anchor);
		}
		else
		{
			var frameworkElement = VirtualView.Parent.ToNative(mauiContext);
			frameworkElement.ContextFlyout = this;
			SetAttachedFlyout(frameworkElement, this);
			ShowAttachedFlyout(frameworkElement);
		}

		VirtualView?.OnOpened();
	}

	void SetDialogPosition(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions)
	{
		if (IsTopLeft())
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		}
		else if (IsTop())
		{
			Placement = FlyoutPlacementMode.Top;
		}
		else if (IsTopRight())
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedRight;
		}
		else if (IsRight())
		{
			Placement = FlyoutPlacementMode.Right;
		}
		else if (IsBottomRight())
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
		}
		else if (IsBottom())
		{
			Placement = FlyoutPlacementMode.Bottom;
		}
		else if (IsBottomLeft())
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
		}
		else if (IsLeft())
		{
			Placement = FlyoutPlacementMode.Left;
		}
		else if (VirtualView != null && VirtualView.Anchor == null)
		{
			Placement = FlyoutPlacementMode.Full;
		}
		else
		{
			Placement = FlyoutPlacementMode.Top;
		}

		bool IsTopLeft() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Start;
		bool IsTop() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Center;
		bool IsTopRight() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.End;
		bool IsRight() => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.End;
		bool IsBottomRight() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.End;
		bool IsBottom() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Center;
		bool IsBottomLeft() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Start;
		bool IsLeft() => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Start;
	}

	void OnClosing(object? sender, FlyoutBaseClosingEventArgs e)
	{
		var isLightDismissEnabled = VirtualView?.IsLightDismissEnabled is true;
		if (!isLightDismissEnabled)
		{
			e.Cancel = true;
		}

		if (IsOpen && isLightDismissEnabled)
		{
			VirtualView?.Handler?.Invoke(nameof(IBasePopup.LightDismiss));
		}
	}

	public void CleanUp()
	{
		if (Control is not null)
		{
			Control.CleanUp();
		}

		VirtualView = null;
		Control = null;

		Closing -= OnClosing;
	}
}