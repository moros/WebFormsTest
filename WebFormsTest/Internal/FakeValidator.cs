using System.Web.UI;

namespace Fritz.WebFormsTest.Internal
{
    internal class FakeValidator : IValidator
    {
        private readonly bool _isValid;

        public FakeValidator(bool isValid)
        {
            _isValid = isValid;
            Validate();
        }

        public void Validate()
        {
            IsValid = _isValid;
        }

        public bool IsValid { get; set; }

        public string ErrorMessage { get; set; }
    }
}
