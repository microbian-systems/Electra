namespace Aero.Core.Validation;

internal interface IFieldValidator
{
    IEnumerable<ValidationError> GetErrors();
}