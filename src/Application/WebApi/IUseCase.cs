namespace Application.WebApi;

public interface IUseCase<in Input, out Output>
{
    Output Execute(Input input);
}
