

using DivinityModManager.Models.Updates;
using DivinityModManager.Util;
using DivinityModManager.Views;

using DynamicData;
using DynamicData.Binding;

using Ookii.Dialogs.Wpf;

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;

public struct CopyModUpdatesTask
{
	public List<string> NewFilesToMove;
	public List<string> UpdatesToMove;
	public string DocumentsFolder;
	public string ModPakFolder;
	public int TotalMoved;
}

public class ModUpdatesViewData : ReactiveObject
{
	[Reactive] public bool Unlocked { get; set; }
	[Reactive] public bool JustUpdated { get; set; }

	public SourceList<DivinityModUpdateData> Mods { get; private set; } = new SourceList<DivinityModUpdateData>();

	private readonly ReadOnlyObservableCollection<DivinityModUpdateData> _newMods;
	public ReadOnlyObservableCollection<DivinityModUpdateData> NewMods => _newMods;

	private readonly ReadOnlyObservableCollection<DivinityModUpdateData> _updatedMods;
	public ReadOnlyObservableCollection<DivinityModUpdateData> UpdatedMods => _updatedMods;

	readonly ObservableAsPropertyHelper<bool> _anySelected;
	public bool AnySelected => _anySelected.Value;

	readonly ObservableAsPropertyHelper<bool> _allNewModsSelected;
	public bool AllNewModsSelected => _allNewModsSelected.Value;

	readonly ObservableAsPropertyHelper<bool> _allModUpdatesSelected;
	public bool AllModUpdatesSelected => _allModUpdatesSelected.Value;

	readonly ObservableAsPropertyHelper<bool> _newAvailable;
	public bool NewAvailable => _newAvailable.Value;

	readonly ObservableAsPropertyHelper<bool> _updatesAvailable;
	public bool UpdatesAvailable => _updatesAvailable.Value;

	readonly ObservableAsPropertyHelper<int> _totalUpdates;
	public int TotalUpdates => _totalUpdates.Value;

	public ICommand CopySelectedModsCommand { get; private set; }
	public ICommand SelectAllNewModsCommand { get; private set; }
	public ICommand SelectAllUpdatesCommand { get; private set; }

	public Action OnLoaded { get; set; }

	public Action<bool> CloseView { get; set; }

	private readonly MainWindowViewModel _mainWindowViewModel;

	public void Clear()
	{
		Mods.Clear();
		Unlocked = true;
	}

	public void SelectAll(bool select = true)
	{
		foreach (var x in Mods.Items)
		{
			x.IsSelected = select;
		}
	}

	private IEnumerable<string> GetUpdateFiles(string directoryPath)
	{
		var files = DivinityFileUtils.EnumerateFiles(directoryPath, DivinityFileUtils.RecursiveOptions, f => Path.GetExtension(f).Equals(".pak", StringComparison.OrdinalIgnoreCase));
		return files;
	}

	private static string GetUniqueBackupPath(string backupFolder, string sourcePath)
	{
		var candidate = Path.Combine(backupFolder, Path.GetFileName(sourcePath));
		if (!File.Exists(candidate)) return candidate;
		var name = Path.GetFileNameWithoutExtension(sourcePath);
		var extension = Path.GetExtension(sourcePath);
		var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		candidate = Path.Combine(backupFolder, $"{name}_{timestamp}{extension}");
		var suffix = 1;
		while (File.Exists(candidate))
			candidate = Path.Combine(backupFolder, $"{name}_{timestamp}_{suffix++}{extension}");
		return candidate;
	}

	private void CopySelectedMods_Run()
	{
		string documentsFolder = _mainWindowViewModel.PathwayData.AppDataGameFolder;
		string modPakFolder = _mainWindowViewModel.PathwayData.AppDataModsPath;

		if (Directory.Exists(modPakFolder))
		{
			Unlocked = false;
			using ProgressDialog dialog = new ProgressDialog()
			{
				WindowTitle = "Updating Mods",
				Text = "Copying mods...",
				CancellationText = "Update Cancelled",
				MinimizeBox = false,
				ProgressBarStyle = ProgressBarStyle.ProgressBar
			};
			dialog.DoWork += CopyFilesProgress_DoWork;
			dialog.RunWorkerCompleted += CopyFilesProgress_RunWorkerCompleted;

			var args = new CopyModUpdatesTask()
			{
				DocumentsFolder = documentsFolder,
				ModPakFolder = modPakFolder,
				NewFilesToMove = NewMods.Where(x => x.IsSelected).Select(x => GetUpdateFiles(Path.GetDirectoryName(x.UpdateFilePath))).SelectMany(x => x).ToList(),
				UpdatesToMove = UpdatedMods.Where(x => x.IsSelected).Select(x => GetUpdateFiles(Path.GetDirectoryName(x.UpdateFilePath))).SelectMany(x => x).ToList(),
				TotalMoved = 0
			};

			dialog.ShowDialog(MainWindow.Self, args);
		}
		else
		{
			CloseView?.Invoke(false);
		}
	}

	public void CopySelectedMods()
	{
		using var dialog = new TaskDialog()
		{
			Buttons =
				{
					new TaskDialogButton(ButtonType.Yes),
					new TaskDialogButton(ButtonType.No)
				},
			WindowTitle = "Update Mods?",
			Content = "Override local mods with the selected updates?",
			MainIcon = TaskDialogIcon.Warning
		};
		var result = dialog.ShowDialog(MainWindow.Self);
		if (result.ButtonType == ButtonType.Yes)
		{
			CopySelectedMods_Run();
		}
	}

	private void CopyFilesProgress_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
	{
		Unlocked = true;
		DivinityApp.Log("Mod updating complete.");
		try
		{
			if (e.Result is CopyModUpdatesTask args)
			{
				JustUpdated = args.TotalMoved > 0;
			}
		}
		catch (Exception ex)
		{
			string message = $"Error copying mods: {ex}";
			DivinityApp.Log(message);
			MainWindow.Self.AlertBar.SetDangerAlert(message);
		}
		CloseView?.Invoke(true);
	}

	private void CopyFilesProgress_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
	{
		ProgressDialog dialog = (ProgressDialog)sender;
		if (e.Argument is CopyModUpdatesTask args)
		{
			var totalWork = args.NewFilesToMove.Count + args.UpdatesToMove.Count;
			string backupFolder = Path.Combine(_mainWindowViewModel.PathwayData.AppDataGameFolder, "Mods_Old_ModManager");
			Directory.CreateDirectory(backupFolder);
			if (args.NewFilesToMove.Count > 0)
			{
				DivinityApp.Log($"Copying '{args.NewFilesToMove.Count}' new mod(s) to the local mods folder.");

				foreach (string file in args.NewFilesToMove)
				{
					if (e.Cancel) return;
					var fileName = Path.GetFileName(file);
					dialog.ReportProgress(args.TotalMoved / totalWork, $"Copying '{fileName}'...", null);
					try
					{
						var destinationPath = Path.Combine(args.ModPakFolder, fileName);
						if (File.Exists(destinationPath))
						{
							var backupPath = GetUniqueBackupPath(backupFolder, destinationPath);
							File.Copy(destinationPath, backupPath, false);
							DivinityApp.Log($"Backed up installed mod '{destinationPath}' to '{backupPath}'.");
						}
						File.Copy(file, destinationPath, true);
					}
					catch (Exception ex)
					{
						string message = $"Error copying '{fileName}':\n{ex}";
						DivinityApp.Log(message);
						MainWindow.Self.AlertBar.SetDangerAlert(message);
						dialog.ReportProgress(args.TotalMoved / totalWork, message, null);
					}
					args.TotalMoved++;
				}
			}

			if (args.UpdatesToMove.Count > 0)
			{
				DivinityApp.Log($"Copying '{args.UpdatesToMove.Count}' mod update(s) to the local mods folder.");
				foreach (string file in args.UpdatesToMove)
				{
					if (e.Cancel) return;
					string baseName = Path.GetFileName(file);
					try
					{
						var destinationPath = Path.Combine(args.ModPakFolder, baseName);
						if (File.Exists(destinationPath))
						{
							var backupPath = GetUniqueBackupPath(backupFolder, destinationPath);
							File.Copy(destinationPath, backupPath, false);
							DivinityApp.Log($"Backed up installed mod '{destinationPath}' to '{backupPath}'.");
						}
						DivinityApp.Log($"Moving mod into mods folder: '{file}'.");
						File.Copy(file, destinationPath, true);
					}
					catch (Exception ex)
					{
						var message = $"Could not back up and update '{baseName}'. The installed mod was left unchanged.\n{ex}";
						DivinityApp.Log(message);
						MainWindow.Self.AlertBar.SetDangerAlert(message);
						dialog.ReportProgress(args.TotalMoved / totalWork, message, null);
					}
					dialog.ReportProgress(args.TotalMoved / totalWork, $"Copying '{baseName}'...", null);
					args.TotalMoved++;
				}
			}
		}

	}

	public ModUpdatesViewData(MainWindowViewModel mainWindowViewModel)
	{
		Unlocked = true;

		_mainWindowViewModel = mainWindowViewModel;

		var modsConnection = Mods.Connect();

		_totalUpdates = modsConnection.Count().ToProperty(this, nameof(TotalUpdates));

		var splitList = modsConnection.AutoRefresh(x => x.IsNewMod);
		var newModsConnection = splitList.Filter(x => x.IsNewMod);
		var updatedModsConnection = splitList.Filter(x => !x.IsNewMod);

		newModsConnection.Bind(out _newMods).Subscribe();
		updatedModsConnection.Bind(out _updatedMods).Subscribe();

		var hasNewMods = newModsConnection.Count().Select(x => x > 0);
		var hasUpdatedMods = updatedModsConnection.Count().Select(x => x > 0);
		_newAvailable = hasNewMods.ToProperty(this, nameof(NewAvailable));
		_updatesAvailable = hasUpdatedMods.ToProperty(this, nameof(UpdatesAvailable));

		var selectedMods = modsConnection.AutoRefresh(x => x.IsSelected).ToCollection();
		_anySelected = selectedMods.Select(x => x.Any(y => y.IsSelected)).ToProperty(this, nameof(AnySelected), true, RxApp.MainThreadScheduler);

		var newModsChangeSet = NewMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection();
		var modUpdatesChangeSet = UpdatedMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection();

		_allNewModsSelected = splitList.Filter(x => x.IsNewMod).ToCollection().Select(x => x.All(y => y.IsSelected)).ToProperty(this, nameof(AllNewModsSelected), true, RxApp.MainThreadScheduler);
		_allModUpdatesSelected = splitList.Filter(x => !x.IsNewMod).ToCollection().Select(x => x.All(y => y.IsSelected)).ToProperty(this, nameof(AllModUpdatesSelected), true, RxApp.MainThreadScheduler);

		var anySelectedObservable = this.WhenAnyValue(x => x.AnySelected);

		CopySelectedModsCommand = ReactiveCommand.Create(CopySelectedMods, anySelectedObservable);

		SelectAllNewModsCommand = ReactiveCommand.Create<bool>((b) =>
		{
			foreach (var x in NewMods)
			{
				x.IsSelected = b;
			}
		}, hasNewMods);
		SelectAllUpdatesCommand = ReactiveCommand.Create<bool>((b) =>
		{
			foreach (var x in UpdatedMods)
			{
				x.IsSelected = b;
			}
		}, hasUpdatedMods);
	}
}
