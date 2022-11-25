namespace Hasher;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Menees.Windows.Presentation;

#endregion

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	#region Constructors

	public App()
	{
		WindowsUtility.InitializeApplication(nameof(Hasher), null);
	}

	#endregion

	#region Internal Properties

	internal Arguments? Arguments { get; private set; }

	#endregion

	#region Protected Methods

	protected override void OnStartup(StartupEventArgs e)
	{
		this.Arguments = new(e.Args);
		base.OnStartup(e);
	}

	#endregion
}
