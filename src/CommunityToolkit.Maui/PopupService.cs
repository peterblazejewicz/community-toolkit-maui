﻿using CommunityToolkit.Maui.Views;
using System.ComponentModel;

namespace CommunityToolkit.Maui;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	readonly IServiceProvider serviceProvider;

	static readonly IDictionary<Type, Type> viewModelToViewMappings = new Dictionary<Type, Type>();

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	public PopupService(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	internal static void AddTransientPopup<TPopupView, TPopupViewModel>(IServiceCollection services)
		where TPopupView : Popup
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient(typeof(TPopupView));
		services.AddTransient(typeof(TPopupViewModel));
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}()"/>
	public void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged =>
		ShowPopup(GetViewModel<TViewModel>());

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(TViewModel)"/>
	public void ShowPopup<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		var popup = GetPopup(typeof(TViewModel));

		GetMainPage().ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(IDictionary{string, object})"/>
	public void ShowPopup<TViewModel>(IDictionary<string, object> query) where TViewModel : IQueryAttributable
	{
		ArgumentNullException.ThrowIfNull(query);

		var popup = GetPopup(typeof(TViewModel));

		if (popup.BindingContext is null)
		{
			var viewModel = GetViewModel<TViewModel>();
			viewModel.ApplyQueryAttributes(query);
			popup.BindingContext = viewModel;
		}

		GetMainPage().ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}()"/>
	public Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged =>
		ShowPopupAsync(GetViewModel<TViewModel>());

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(TViewModel)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		var popup = GetPopup(typeof(TViewModel));

		return GetMainPage().ShowPopupAsync(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(IDictionary{string, object})"/>
	public Task<object?> ShowPopupAsync<TViewModel>(IDictionary<string, object> query) where TViewModel : IQueryAttributable
	{
		ArgumentNullException.ThrowIfNull(query);

		var popup = GetPopup(typeof(TViewModel));

		if (popup.BindingContext is null)
		{
			var viewModel = GetViewModel<TViewModel>();
			viewModel.ApplyQueryAttributes(query);
			popup.BindingContext = viewModel;
		}

		return GetMainPage().ShowPopupAsync(popup);
	}

	static Page GetMainPage()
	{
		var currentApplication = Application.Current;

		ArgumentNullException.ThrowIfNull(currentApplication);

		var mainPage = currentApplication.MainPage;

		ArgumentNullException.ThrowIfNull(mainPage);

		return mainPage;
	}

	Popup GetPopup(Type viewModelType)
	{
		var popup = this.serviceProvider.GetService(viewModelToViewMappings[viewModelType]) as Popup;

		if (popup is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve popup type for {viewModelType} please make sure that you have called {nameof(AddTransientPopup)}");
		}

		return popup;
	}

	TViewModel GetViewModel<TViewModel>()
	{
		var viewModel = this.serviceProvider.GetService<TViewModel>();

		if (viewModel is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve type {typeof(TViewModel)} please make sure that you have called {nameof(AddTransientPopup)}");
		}

		return viewModel;
	}
}
