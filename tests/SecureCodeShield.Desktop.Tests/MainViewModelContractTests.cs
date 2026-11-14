using System.Reflection;
using SecureCodeShield.Desktop.ViewModels;

namespace SecureCodeShield.Desktop.Tests;

public sealed class MainViewModelContractTests
{
    [Fact]
    public void Default_workspace_exposes_product_context_dashboard_and_local_engine_readiness()
    {
        var viewModel = new MainViewModel();
        var viewModelType = viewModel.GetType();

        var productName = viewModelType.GetProperty("ProductName", BindingFlags.Public | BindingFlags.Instance);
        var currentSection = viewModelType.GetProperty("CurrentSection", BindingFlags.Public | BindingFlags.Instance);
        var isLocalEngineReady = viewModelType.GetProperty("IsLocalEngineReady", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(productName is not null, "The workstation state must expose ProductName.");
        Assert.True(currentSection is not null, "The workstation state must expose CurrentSection.");
        Assert.True(isLocalEngineReady is not null, "The workstation state must expose IsLocalEngineReady.");
        if (productName is null || currentSection is null || isLocalEngineReady is null)
        {
            return;
        }

        Assert.Equal("Secure Code Shield", productName.GetValue(viewModel));
        Assert.Equal("Dashboard", currentSection.GetValue(viewModel));
        Assert.Equal(true, isLocalEngineReady.GetValue(viewModel));
    }

    [Fact]
    public void Local_review_remains_blocked_until_the_selected_project_is_explicitly_authorized()
    {
        var viewModel = new MainViewModel();

        viewModel.SetProjectCandidate("authorized-local-fixture");

        Assert.False(viewModel.CanStartLocalReview);
        Assert.Equal("Authorization required", viewModel.ReviewReadiness);

        viewModel.AuthorizeSelectedProject();

        Assert.True(viewModel.CanStartLocalReview);
        Assert.Equal("Ready for local review", viewModel.ReviewReadiness);
        Assert.Equal("Ready to start a local review", viewModel.ScanStatus);
    }

    [Fact]
    public void Completed_local_review_exposes_safe_summary_and_report_readiness()
    {
        var viewModel = new MainViewModel();
        viewModel.SetProjectCandidate("authorized-local-fixture");
        viewModel.AuthorizeSelectedProject();

        viewModel.BeginLocalReview();

        Assert.True(viewModel.IsLocalReviewRunning);
        Assert.False(viewModel.CanExportSanitizedReport);
        Assert.Equal("Local review in progress", viewModel.ReviewReadiness);

        viewModel.CompleteLocalReview(2);

        Assert.False(viewModel.IsLocalReviewRunning);
        Assert.True(viewModel.CanExportSanitizedReport);
        Assert.Equal(2, viewModel.FindingCount);
        Assert.Equal("Local review completed", viewModel.ReviewReadiness);
    }

    [Fact]
    public void Cancelled_local_review_does_not_enable_a_sanitized_report()
    {
        var viewModel = new MainViewModel();
        viewModel.SetProjectCandidate("authorized-local-fixture");
        viewModel.AuthorizeSelectedProject();
        viewModel.BeginLocalReview();

        viewModel.CancelLocalReview();

        Assert.False(viewModel.IsLocalReviewRunning);
        Assert.False(viewModel.CanExportSanitizedReport);
        Assert.Equal("Local review cancelled", viewModel.ReviewReadiness);
    }
}
