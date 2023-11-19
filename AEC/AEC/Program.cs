using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.ComponentModel;
using SimpleInjector;
using Container = SimpleInjector.Container;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


/**
 * @mainpage Sobre o teste
 * Olá amigos AEC, esta pequena introdução é referente ao exercício Desafio AeC Automação.pdf.
 * Aqui nestes scripts obedeci as regras utilizando a injeção de dependencias com SimpleInjector package
 * Package WebDriver Selenium para navegar
 * Package DotNetHelpers interagir elementos e utilizar o Expected Conditions pois o Selenium acima 4.0 nao usa mais
 * Package Microsoft.EntityFrameworkCore´para criar um banco de dados virtual dentro do VS2022, ou seja 
 * para não demorar para a entrega do teste preferi criar algo virtual e mostrar no console o resultado que foi injetado
 * os dados no Database Memory do VS2022
 * Eu inseri detalhado na mesma classe principal Program.cs pois está conforme esta linha do pdf
 * Você não precisa se preocupar com o design. Esse não é o objetivo do desafio.
 * Apenas clique em executar lembrando de ver as propriedades optando para Console Application
 * 
 * Obrigado pela atenção.
 */


//criando DTO de dados para montar minha table
public class CursoDTO
{
    public int CursoId { get; set; } //esta é a chave primária
    public string Titulo { get; set; }
    public string Professor { get; set; }
    public string CargaHoraria { get; set; }
    public string Descricao { get; set; }
}

//criando uma herança do DbContext para representar o bando de dados
public class CursoContext : DbContext
{
    public DbSet<CursoDTO> Cursos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //aqui estou declarando nome da virtual table
        optionsBuilder.UseInMemoryDatabase("AEC");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelando o ID para poder inserir o restante do DTO
        modelBuilder.Entity<CursoDTO>().HasKey(c => c.CursoId);
    }
}

// Definição da interface do DDD do serviço de pesquisa
public interface ISearchService
{
    CursoDTO Search(IWebDriver driver, string searchCourse);
}

// Implementação do código do serviço que ira fazer no site Alura utilizando o serviço
public class AluraSearchService : ISearchService
{
    //declarando o pageobjects
    private By textBoxSearch = By.Id("header-barraBusca-form-campoBusca");
    private By linkTitle = By.ClassName("busca-resultado-nome");
    private By innerTextHour = By.XPath("/html[1]/body[1]/main[1]/section[1]/article[2]/div[1]/div[1]/div[2]/div[1]");
    private By innerTextDescription = By.ClassName("formacao-descricao-texto");
    private By innerTextTeacher = By.ClassName("formacao-instrutor-nome");

    public CursoDTO Search(IWebDriver driver, string searchCourse)
    {
        //usei o dotnethelpers para waitelements no selenium 4 nao existe mais
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        driver.Navigate().GoToUrl("https://www.alura.com.br/");
        driver.Manage().Window.Maximize();

        driver.FindElement(textBoxSearch).SendKeys(searchCourse);
        driver.FindElement(textBoxSearch).Submit();

        wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(linkTitle));
        var linkTitleList = driver.FindElements(linkTitle);
        IWebElement firstTitle = linkTitleList.FirstOrDefault();
        firstTitle.Click();

        wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(innerTextHour));
        var linkTeacherList = driver.FindElements(innerTextTeacher);
        IWebElement firstTeacher = linkTeacherList.FirstOrDefault();
        string teacher = firstTeacher.GetAttribute("innerText").Trim();
        string hour = driver.FindElement(innerTextHour).GetAttribute("innerText").Replace("h", "");
        string description = driver.FindElement(innerTextDescription).GetAttribute("innerText").Trim();

        //inserindo valores nas varíáveis DTO
        CursoDTO curso = new CursoDTO
        {
            Titulo = searchCourse,
            Professor = teacher,
            CargaHoraria = hour,
            Descricao = description
        };

        return curso;
    }
}

//classe principal que irá executar
class Program
{
    static void Main(string[] args)
    {

        //nesta linha estou passando os resultados AluraSearchService para dentro da interface do CurstoDTO
        ISearchService searchService = new AluraSearchService();

        using (IWebDriver driver = new ChromeDriver())
        {
            try
            {
                string searchCourseWithName = "Formação Modelagem e Melhorias de Processos de Negócios";
                CursoDTO curso = searchService.Search(driver, searchCourseWithName);

                using (var context = new CursoContext())
                {
                    context.Cursos.Add(curso);
                    context.SaveChanges();
                }
                // vou verificar no console do VS2022 se o curso foi inserido no banco de dados
                //PS: conforme lá em cima descrito as informações injetadas estão no memory Database VS2022
                using (var context = new CursoContext())
                {
                    var cursoInserido = context.Cursos.FirstOrDefault(c => c.Titulo == searchCourseWithName);

                    if (cursoInserido != null)
                    {
                        Console.WriteLine("CURSO INSERIDO COM SUCESSO NO DATABASE MEMORY VS!\n");
                        Console.WriteLine($"ID do Curso: {cursoInserido.CursoId}\n");
                        Console.WriteLine($"Título: {cursoInserido.Titulo}\n");
                        Console.WriteLine($"Instrutor: {cursoInserido.Professor} \n");
                        Console.WriteLine($"Carga horária: {cursoInserido.CargaHoraria} \n");
                        Console.WriteLine($"Descrição: {cursoInserido.Descricao}\n");
                    }
                    else
                    {
                        Console.WriteLine("Falha ao inserir o curso no banco de dados.");
                    }
                }

                // Consulta e imprime os cursos no console, aqui só fiz para listar mas as respostas são as
                // mesmas comentadas acima
                //using (var context = new CursoContext())
                //{
                //    var cursos = context.Cursos.ToList();

                //    foreach (var c in cursos)
                //    {
                //        Console.WriteLine($" ID: {c.CursoId},\n Título: {c.Titulo},\n Professor: {c.Professor},\n Carga Horária: {c.CargaHoraria}, Descrição: {c.Descricao}");
                //    }
                //}
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}


