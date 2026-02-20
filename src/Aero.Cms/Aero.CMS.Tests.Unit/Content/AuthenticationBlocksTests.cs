using Aero.CMS.Core.Content.Models.Blocks;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class AuthenticationBlocksTests
{
    [Fact]
    public void LoginBlock_Type_Is_loginBlock()
    {
        var block = new LoginBlock();
        block.Type.ShouldBe("loginBlock");
    }

    [Fact]
    public void LoginBlock_Title_RoundTrips()
    {
        var block = new LoginBlock();
        block.Title = "Welcome Back";
        block.Title.ShouldBe("Welcome Back");
        block.Properties["title"].ShouldBe("Welcome Back");
    }

    [Fact]
    public void LoginBlock_ShowForgotPasswordLink_RoundTrips()
    {
        var block = new LoginBlock();
        block.ShowForgotPasswordLink = true;
        block.ShowForgotPasswordLink.ShouldBeTrue();
        block.Properties["showForgotPasswordLink"].ShouldBe(true);
    }

    [Fact]
    public void LoginBlock_RedirectUrl_RoundTrips()
    {
        var block = new LoginBlock();
        block.RedirectUrl = "/dashboard";
        block.RedirectUrl.ShouldBe("/dashboard");
        block.Properties["redirectUrl"].ShouldBe("/dashboard");
    }

    [Fact]
    public void LoginBlock_AuthenticationMode_Defaults_To_Mock()
    {
        var block = new LoginBlock();
        block.AuthenticationMode.ShouldBe("Mock");
    }

    [Fact]
    public void RegisterBlock_Type_Is_registerBlock()
    {
        var block = new RegisterBlock();
        block.Type.ShouldBe("registerBlock");
    }

    [Fact]
    public void RegisterBlock_RequireEmailConfirmation_Defaults_To_True()
    {
        var block = new RegisterBlock();
        block.RequireEmailConfirmation.ShouldBeTrue();
    }

    [Fact]
    public void RegisterBlock_Title_RoundTrips()
    {
        var block = new RegisterBlock();
        block.Title = "Create Account";
        block.Title.ShouldBe("Create Account");
        block.Properties["title"].ShouldBe("Create Account");
    }

    [Fact]
    public void RegisterBlock_TermsUrl_RoundTrips()
    {
        var block = new RegisterBlock();
        block.TermsUrl = "/terms";
        block.TermsUrl.ShouldBe("/terms");
        block.Properties["termsUrl"].ShouldBe("/terms");
    }

    [Fact]
    public void RegisterBlock_ValidatePasswordStrength_Defaults_To_False()
    {
        var block = new RegisterBlock();
        block.ValidatePasswordStrength.ShouldBeFalse();
    }

    [Fact]
    public void RegisterBlock_PasswordMinLength_Defaults_To_8()
    {
        var block = new RegisterBlock();
        block.PasswordMinLength.ShouldBe(8);
    }

    [Fact]
    public void ForgotPasswordBlock_Type_Is_forgotPasswordBlock()
    {
        var block = new ForgotPasswordBlock();
        block.Type.ShouldBe("forgotPasswordBlock");
    }

    [Fact]
    public void ForgotPasswordBlock_SuccessMessage_Returns_Default_When_Absent()
    {
        var block = new ForgotPasswordBlock();
        block.SuccessMessage.ShouldBe("Check your email for password reset instructions.");
    }

    [Fact]
    public void ForgotPasswordBlock_ErrorMessage_Returns_Default_When_Absent()
    {
        var block = new ForgotPasswordBlock();
        block.ErrorMessage.ShouldBe("An error occurred. Please try again.");
    }

    [Fact]
    public void ForgotPasswordBlock_EmailFieldLabel_Defaults_To_Email_Address()
    {
        var block = new ForgotPasswordBlock();
        block.EmailFieldLabel.ShouldBe("Email Address");
    }

    [Fact]
    public void All_Blocks_Have_NonEmpty_Guid_Id()
    {
        var loginBlock = new LoginBlock();
        var registerBlock = new RegisterBlock();
        var forgotPasswordBlock = new ForgotPasswordBlock();

        loginBlock.Id.ShouldNotBe(Guid.Empty);
        registerBlock.Id.ShouldNotBe(Guid.Empty);
        forgotPasswordBlock.Id.ShouldNotBe(Guid.Empty);
    }
}