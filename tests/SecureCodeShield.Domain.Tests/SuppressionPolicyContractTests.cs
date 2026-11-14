using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class SuppressionPolicyContractTests
{
    [Theory]
    [InlineData("tests/auth.spec.ts")]
    [InlineData("src/__tests__/login.ts")]
    [InlineData("fixtures/keys.json")]
    [InlineData("examples/quickstart.js")]
    [InlineData("src/services/auth.spec.ts")]
    [InlineData(".env.example")]
    public void Treats_a_non_production_path_as_out_of_scope(string relativePath)
    {
        Assert.True(SuppressionPolicy.IsNonProductionPath(relativePath), relativePath);
    }

    [Theory]
    [InlineData("src/services/auth.ts")]
    [InlineData("src/config/db.ts")]
    [InlineData("app/latest/handler.py")]
    public void Keeps_production_code_in_scope(string relativePath)
    {
        Assert.False(SuppressionPolicy.IsNonProductionPath(relativePath), relativePath);
    }

    [Fact]
    public void A_bare_marker_accepts_the_whole_line()
    {
        Assert.True(SuppressionPolicy.IsSuppressedInline("const k = \"…\"; // scs-ignore", RuleCatalog.HighEntropyAssignment));
    }

    [Fact]
    public void A_targeted_marker_accepts_only_the_named_rule()
    {
        const string line = "const sql = \"SELECT 1 FROM t WHERE a = \" + a; // scs-ignore SCS-CODE-004";

        Assert.True(SuppressionPolicy.IsSuppressedInline(line, RuleCatalog.SqlBuiltByConcatenation));
        Assert.False(SuppressionPolicy.IsSuppressedInline(line, RuleCatalog.HighEntropyAssignment));
    }

    [Fact]
    public void An_unmarked_line_is_never_suppressed()
    {
        Assert.False(SuppressionPolicy.IsSuppressedInline("const k = \"value\";", RuleCatalog.HighEntropyAssignment));
    }
}
