using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureCodeShield.Domain.Findings;
using SecureCodeShield.Infrastructure.LocalFiles;

namespace SecureCodeShield.Desktop.Review;

public sealed class LocalReviewRunner
{
    public Task<IReadOnlyList<SecretFinding>> RunAsync(string projectRoot, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        return Task.Run(
            () => LocalProjectScanner.ScanCancellable(projectRoot, explicitAuthorization: true, cancellationToken),
            cancellationToken);
    }
}
