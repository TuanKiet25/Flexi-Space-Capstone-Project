namespace FlexiSpace.Application.ViewModels.Responses.Space
{
    public class AddressOptionRP
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class AddressNodeRP : AddressOptionRP
    {
        public List<AddressNodeRP> Children { get; set; } = new();
    }
}