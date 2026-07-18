using TaxSystem.BankService.Repositories;
using TaxSystem.CitizenService.Repositories;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;
using TaxSystem.StatementGenerator.Repositories;

namespace TaxSystem.Tests.ServiceTests;

public class RepositoryPersistenceTests
{
    private string _dataPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _dataPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "repository-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, recursive: true);
        }
    }

    [Test]
    public async Task CitizenServiceRepositoryPersistsCitizen()
    {
        var repository = new CitizenRepository(new FileSystemRepository("citizens", _dataPath));
        IReadCitizenRepository readRepository = repository;
        IWriteCitizenRepository writeRepository = repository;
        var citizen = new Citizen
        {
            cpr = "0101011234",
            firstName = "John",
            lastName = "Doe",
            streetAddress = "Main Street 1",
            city = "Copenhagen",
            zipCode = "1000"
        };

        await writeRepository.SaveAsync(citizen);

        var persistedCitizen = await readRepository.GetByCprAsync(citizen.cpr);
        Assert.That(persistedCitizen, Is.Not.Null);
        Assert.That(persistedCitizen!.cpr, Is.EqualTo(citizen.cpr));
        Assert.That(persistedCitizen.firstName, Is.EqualTo(citizen.firstName));
    }

    [Test]
    public async Task CompanyServiceRepositoryPersistsCompany()
    {
        var repository = new CompanyRepository(new FileSystemRepository("companies", _dataPath));
        IReadCompanyRepository readRepository = repository;
        IWriteCompanyRepository writeRepository = repository;
        var company = new Company
        {
            CVR = "12345678",
            Name = "Acme Corp"
        };

        await writeRepository.SaveAsync(company);

        var persistedCompany = await readRepository.GetByCvrAsync(company.CVR);
        Assert.That(persistedCompany, Is.Not.Null);
        Assert.That(persistedCompany!.CVR, Is.EqualTo(company.CVR));
        Assert.That(persistedCompany.Name, Is.EqualTo(company.Name));
    }

    [Test]
    public async Task BankServiceRepositoryPersistsBankTransfer()
    {
        var repository = new BankRepository(new FileSystemRepository("bank-transfers", _dataPath));
        IBankReadRepository readRepository = repository;
        IBankWriteRepository writeRepository = repository;
        var transfer = new BankTransfer(
            Cpr: "0101011234",
            Amount: 2500m,
            AccountNumber: "1234567890",
            RegistrationNumber: "1234",
            Status: "Scheduled");

        await writeRepository.SaveAsync(transfer);

        var persistedTransfer = await readRepository.GetByCprAsync(transfer.Cpr);
        Assert.That(persistedTransfer, Is.EqualTo(transfer));
    }

    [Test]
    public async Task StatementGeneratorServiceRepositoryPersistsStatement()
    {
        var repository = new StatementRepository(new FileSystemRepository("statements", _dataPath));
        IReadStatementRepository readRepository = repository;
        IWriteStatementRepository writeRepository = repository;
        var cpr = "0101011234";
        var statement = new Statement
        {
            annualGrossSalary = "100000",
            annualCapitalGains = "1000",
            annualTotalDeduction = "5000",
            annualPaidTax = "25000",
            annualTax = "30000",
            annualOwedTax = "5000"
        };

        await writeRepository.SaveAsync(cpr, statement);

        var persistedStatement = await readRepository.GetByCprAsync(cpr);
        Assert.That(persistedStatement, Is.Not.Null);
        Assert.That(persistedStatement!.annualGrossSalary, Is.EqualTo(statement.annualGrossSalary));
        Assert.That(persistedStatement.annualOwedTax, Is.EqualTo(statement.annualOwedTax));
    }
}
