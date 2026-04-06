namespace Apia;

public interface IQuery<T>
{
    void Accept(IQueryVisitor<T> visitor);
}
