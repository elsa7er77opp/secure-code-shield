using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SecureCodeShield.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string ProductName => "Secure Code Shield";

    public string CurrentSection => "Dashboard";

    public bool IsLocalEngineReady => true;

    [ObservableProperty]
    public partial string ProjectContext { get; set; } = "No authorized project open";

    [ObservableProperty]
    public partial string ScanStatus { get; set; } = "Authorization required";

    [ObservableProperty]
    public partial string ReviewReadiness { get; set; } = "Authorization required";

    [ObservableProperty]
    public partial bool IsProjectAuthorized { get; set; }

    [ObservableProperty]
    public partial bool IsLocalReviewRunning { get; set; }

    [ObservableProperty]
    public partial bool CanExportSanitizedReport { get; set; }

    [ObservableProperty]
    public partial int FindingCount { get; set; }

    public bool CanStartLocalReview => IsProjectAuthorized;

    public void SetProjectCandidate(string folderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        ProjectContext = folderName + " — authorization required";
        ScanStatus = "Folder selected — explicit authorization required";
        ReviewReadiness = "Authorization required";
        IsProjectAuthorized = false;
        IsLocalReviewRunning = false;
        CanExportSanitizedReport = false;
        FindingCount = 0;
    }

    public void AuthorizeSelectedProject()
    {
        if (ProjectContext == "No authorized project open")
        {
            throw new InvalidOperationException("Choose a local project before authorizing a review.");
        }

        IsProjectAuthorized = true;
        ReviewReadiness = "Ready for local review";
        ScanStatus = "Ready to start a local review";
    }

    public void BeginLocalReview()
    {
        if (!CanStartLocalReview)
        {
            throw new InvalidOperationException("Explicit authorization is required before a local review can begin.");
        }

        IsLocalReviewRunning = true;
        CanExportSanitizedReport = false;
        FindingCount = 0;
        ReviewReadiness = "Local review in progress";
        ScanStatus = "Reading eligible local text files without execution";
    }

    public void CompleteLocalReview(int findingCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(findingCount);

        IsLocalReviewRunning = false;
        FindingCount = findingCount;
        CanExportSanitizedReport = true;
        ReviewReadiness = "Local review completed";
        ScanStatus = "Redacted findings are ready for local review";
    }

    public void CancelLocalReview()
    {
        IsLocalReviewRunning = false;
        CanExportSanitizedReport = false;
        ReviewReadiness = "Local review cancelled";
        ScanStatus = "Local review cancelled before completion";
    }

    partial void OnIsProjectAuthorizedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartLocalReview));
    }
}
