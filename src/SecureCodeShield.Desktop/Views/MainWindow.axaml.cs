using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SecureCodeShield.Desktop.Review;
using SecureCodeShield.Desktop.ViewModels;
using SecureCodeShield.Domain.Findings;
using SecureCodeShield.Infrastructure.Reports;

namespace SecureCodeShield.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly LocalReviewRunner _reviewRunner = new();
    private IReadOnlyList<SecretFinding> _latestFindings = [];
    private CancellationTokenSource? _reviewCancellation;
    private string? _projectRoot;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ChooseProjectClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose a project folder for review",
            AllowMultiple = false
        });

        if (folders.Count == 0 || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var localPath = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.ScanStatus = "The selected folder must be available as a local path.";
            return;
        }

        _projectRoot = localPath;
        _latestFindings = [];
        viewModel.SetProjectCandidate(folders[0].Name);
    }

    private void AuthorizeProjectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        try
        {
            viewModel.AuthorizeSelectedProject();
        }
        catch (InvalidOperationException exception)
        {
            viewModel.ScanStatus = exception.Message;
        }
    }

    private async void StartLocalReviewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || string.IsNullOrWhiteSpace(_projectRoot) || !viewModel.CanStartLocalReview)
        {
            return;
        }

        _reviewCancellation?.Dispose();
        _reviewCancellation = new CancellationTokenSource();
        viewModel.BeginLocalReview();

        try
        {
            _latestFindings = await _reviewRunner.RunAsync(_projectRoot, _reviewCancellation.Token);
            viewModel.CompleteLocalReview(_latestFindings.Count);
        }
        catch (OperationCanceledException)
        {
            viewModel.CancelLocalReview();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            viewModel.CancelLocalReview();
            viewModel.ScanStatus = "Local review could not complete: " + exception.Message;
        }
        finally
        {
            _reviewCancellation?.Dispose();
            _reviewCancellation = null;
        }
    }

    private void CancelLocalReviewClick(object? sender, RoutedEventArgs e)
    {
        _reviewCancellation?.Cancel();
        if (DataContext is MainViewModel viewModel && viewModel.IsLocalReviewRunning)
        {
            viewModel.CancelLocalReview();
        }
    }

    private async void ExportReportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || !viewModel.CanExportSanitizedReport)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose a local folder for the sanitized report",
            AllowMultiple = false
        });

        var outputDirectory = folders.Count == 0 ? null : folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        var report = SanitizedReportExporter.Export(outputDirectory, viewModel.ProjectContext, _latestFindings);
        viewModel.ScanStatus = "Sanitized report saved locally: " + Path.GetFileName(report.ReportPath);
    }
}
