using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxSystem.BankService.Persistance;
using TaxSystem.BankService.Repositories;
using TaxSystem.CitizenService.Persistance;
using TaxSystem.CitizenService.Repositories;
using TaxSystem.CompanyService.Persistance;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Models;
using TaxSystem.StatementGenerator.Persistance;
using TaxSystem.StatementGenerator.Repositories;

namespace TaxSystem.Tests.ServiceTests;

public class RepositoryPersistenceTests
{
    private SqliteConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task CitizenServiceRepositoryPersistsCitizen()
    {
        var options = new DbContextOptionsBuilder<CitizenDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = new CitizenDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new CitizenPostgresRepository(dbContext);
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
        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = new CompanyDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new CompanyPostgresRepository(dbContext);
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
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = new BankDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new BankPostgresRepository(dbContext);
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
        var options = new DbContextOptionsBuilder<StatementDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = new StatementDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new StatementPostgresRepository(dbContext);
        IReadStatementRepository readRepository = repository;
        IWriteStatementRepository writeRepository = repository;
        var cpr = "0101011234";
        var statement = new Statement
        {
            cpr = cpr,
            reportedAt = DateTime.UtcNow,
            annualGrossSalary = "100000",
            annualCapitalGains = "1000",
            annualTotalDeduction = "5000",
            annualPaidTax = "25000",
            annualTax = "30000",
            annualOwedTax = "5000"
        };

        await writeRepository.SaveReportAsync(cpr, statement);

        var persistedStatement = await readRepository.GetMergedStatementAsync(cpr);
        Assert.That(persistedStatement, Is.Not.Null);
        Assert.That(persistedStatement!.annualGrossSalary, Is.EqualTo(statement.annualGrossSalary));
        Assert.That(persistedStatement.annualOwedTax, Is.EqualTo(statement.annualOwedTax));
    }
}
