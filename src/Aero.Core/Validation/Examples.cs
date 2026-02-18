using System.Collections;

namespace Aero.Core.Validation;

public class Examples
{
        class BadRequest(IEnumerable<ValidationError> errors);
    
    public void Test()
    {
        var email = "";
        var password = "";
        var registrationDto = new
        {
            Email = "",
            Username = "",
            Password = "",
            Phone = "",
            Website = ""
        };
        
// ── 1. Standalone single-value validation ───────────────────
ValidationResult emailResult = StringValidator.For(email, "Email")
    .NotEmpty()
    .MustBeEmail()
    .Validate();

ValidationResult passwordResult = StringValidator.For(password, "Password")
    .NotEmpty()
    .MinLength(8)
    .MustBeValidPassword()
    .Validate();

// ── 2. Model validation ─────────────────────────────────────
var result = ModelValidator.For(registrationDto)
    .RuleFor(x => x.Email).NotEmpty().MustBeEmail()
    .RuleFor(x => x.Username).NotEmpty().MustBeValidUsername()
    .RuleFor(x => x.Password).NotEmpty().MustBeValidPassword()
    .RuleFor(x => x.Phone).MustBePhone()                        // optional — skips null
    .RuleFor(x => x.Website).MustBeUrl()
    .Validate();

if (!result.IsValid)
    new BadRequest(result.Errors);

var dto = new
{
    ZipCode = "",
    CardNumber = "",
    Email = ""
};
// ── 3. Custom messages ───────────────────────────────────────
var result2 = ModelValidator.For(dto)
    .RuleFor(x => x.ZipCode)
        .NotEmpty("Zip code is required.")
        .MustBeZipCode("Please enter a valid 5-digit ZIP code.")
    .RuleFor(x => x.CardNumber)
        .NotEmpty()
        .MustBeCreditCard("Invalid card number.")
    .Validate();


var value = "XXX-0000";
// ── 4. Escape hatches ────────────────────────────────────────
var result3 = StringValidator.For(value, "Code")
    .NotEmpty()
    .Matches(@"^[A-Z]{3}-\d{4}$", "Must be in format AAA-0000.")
    .Must(v => v != "XXX-0000", "That code is reserved.")
    .Validate();

// ── 5. Throw on failure ──────────────────────────────────────
ModelValidator.For(dto)
    .RuleFor(x => x.Email).NotEmpty().MustBeEmail()
    .Validate()
    .ThrowIfInvalid();                                          // throws ValidationException
    }
}