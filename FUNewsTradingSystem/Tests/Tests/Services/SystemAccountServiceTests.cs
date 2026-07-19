using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services;
using FUNewsTradingSystem_BusinessLayer.Services.Implements;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem.Tests.Services
{
    public class SystemAccountServiceTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static (SystemAccountService svc, Mock<ISystemAccountRepository> repoMock) Build()
        {
            var mock = new Mock<ISystemAccountRepository>();
            var svc  = new SystemAccountService(mock.Object);
            return (svc, mock);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetByEmailAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByEmailAsync_ExistingEmail_ReturnsAccount()
        {
            var (svc, repo) = Build();
            var account     = new SystemAccount { AccountID = 1, AccountEmail = "staff@fu.edu.vn" };
            repo.Setup(r => r.GetByEmailAsync("staff@fu.edu.vn")).ReturnsAsync(account);

            var result = await svc.GetByEmailAsync("staff@fu.edu.vn");

            result.Should().NotBeNull();
            result!.AccountEmail.Should().Be("staff@fu.edu.vn");
        }

        [Fact]
        public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((SystemAccount?)null);

            var result = await svc.GetByEmailAsync("ghost@fu.edu.vn");

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // AuthenticateAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_CorrectPassword_ReturnsAccount()
        {
            var (svc, repo) = Build();
            var account     = new SystemAccount
            {
                AccountID       = 2,
                AccountEmail    = "admin@fu.edu.vn",
                AccountPassword = "secret123"
            };
            repo.Setup(r => r.GetByEmailAsync("admin@fu.edu.vn")).ReturnsAsync(account);

            var result = await svc.AuthenticateAsync("admin@fu.edu.vn", "secret123");

            result.Should().NotBeNull();
            result!.AccountID.Should().Be(2);
        }

        [Fact]
        public async Task AuthenticateAsync_WrongPassword_ReturnsNull()
        {
            var (svc, repo) = Build();
            var account     = new SystemAccount
            {
                AccountEmail    = "staff@fu.edu.vn",
                AccountPassword = "correctPass"
            };
            repo.Setup(r => r.GetByEmailAsync("staff@fu.edu.vn")).ReturnsAsync(account);

            var result = await svc.AuthenticateAsync("staff@fu.edu.vn", "wrongPass");

            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_AccountNotFound_ReturnsNull()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((SystemAccount?)null);

            var result = await svc.AuthenticateAsync("nobody@fu.edu.vn", "pass");

            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_HashPlaceholder_ReturnsAccount()
        {
            var (svc, repo) = Build();
            var account     = new SystemAccount
            {
                AccountEmail    = "test@fu.edu.vn",
                AccountPassword = "@@abc123@@_HASH_PLACEHOLDER"
            };
            repo.Setup(r => r.GetByEmailAsync("test@fu.edu.vn")).ReturnsAsync(account);

            // Any password should pass when the stored value is the hash placeholder
            var result = await svc.AuthenticateAsync("test@fu.edu.vn", "anyPassword");

            result.Should().NotBeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // GetAllAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsAllAccounts()
        {
            var (svc, repo) = Build();
            var accounts    = new List<SystemAccount>
            {
                new() { AccountID = 1 },
                new() { AccountID = 2 }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);

            var result = await svc.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetByIdAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsAccount()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new SystemAccount { AccountID = 5 });

            var result = await svc.GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.AccountID.Should().Be(5);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SystemAccount?)null);

            var result = await svc.GetByIdAsync(9999);

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // CreateAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NewEmail_CreatesAndReturnsSuccess()
        {
            var (svc, repo) = Build();
            var newAcc      = new SystemAccount { AccountEmail = "new@fu.edu.vn" };
            repo.Setup(r => r.EmailExistsAsync("new@fu.edu.vn", null)).ReturnsAsync(false);
            repo.Setup(r => r.CreateAsync(newAcc))
                .ReturnsAsync(new SystemAccount { AccountID = 10, AccountEmail = "new@fu.edu.vn" });

            var result = await svc.CreateAsync(newAcc);

            result.Success.Should().BeTrue();
            result.EntityId.Should().Be(10);
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ReturnsFailure()
        {
            var (svc, repo) = Build();
            var newAcc      = new SystemAccount { AccountEmail = "dup@fu.edu.vn" };
            repo.Setup(r => r.EmailExistsAsync("dup@fu.edu.vn", null)).ReturnsAsync(true);

            var result = await svc.CreateAsync(newAcc);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Email already exists");
        }

        // ─────────────────────────────────────────────────────────────────
        // UpdateAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_UniqueEmail_UpdatesAndReturnsSuccess()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount { AccountID = 1, AccountEmail = "unique@fu.edu.vn" };
            repo.Setup(r => r.EmailExistsAsync("unique@fu.edu.vn", 1)).ReturnsAsync(false);
            repo.Setup(r => r.UpdateAsync(acc)).Returns(Task.CompletedTask);

            var result = await svc.UpdateAsync(acc);

            result.Success.Should().BeTrue();
            result.EntityId.Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_EmailConflict_ReturnsFailure()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount { AccountID = 2, AccountEmail = "taken@fu.edu.vn" };
            repo.Setup(r => r.EmailExistsAsync("taken@fu.edu.vn", 2)).ReturnsAsync(true);

            var result = await svc.UpdateAsync(acc);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Email already exists");
        }

        // ─────────────────────────────────────────────────────────────────
        // DeleteAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_AlwaysReturnsSuccess()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var result = await svc.DeleteAsync(5);

            result.Success.Should().BeTrue();
        }

        // ─────────────────────────────────────────────────────────────────
        // UpdateNameAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateNameAsync_ExistingAccount_UpdatesNameAndReturnsSuccess()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount { AccountID = 1, AccountName = "Old Name" };
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(acc);
            repo.Setup(r => r.UpdateAsync(It.IsAny<SystemAccount>())).Returns(Task.CompletedTask);

            var result = await svc.UpdateNameAsync(1, "New Name");

            result.Success.Should().BeTrue();
            acc.AccountName.Should().Be("New Name");
        }

        [Fact]
        public async Task UpdateNameAsync_NonExistingAccount_ReturnsFailure()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SystemAccount?)null);

            var result = await svc.UpdateNameAsync(999, "New Name");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Account not found");
        }

        // ─────────────────────────────────────────────────────────────────
        // ChangePasswordAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ChangePasswordAsync_CorrectCurrentPassword_ChangesSuccessfully()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount { AccountID = 3, AccountPassword = "oldPass" };
            repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(acc);
            repo.Setup(r => r.UpdateAsync(It.IsAny<SystemAccount>())).Returns(Task.CompletedTask);

            var result = await svc.ChangePasswordAsync(3, "oldPass", "newPass");

            result.Success.Should().BeTrue();
            acc.AccountPassword.Should().Be("newPass");
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFailure()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount { AccountID = 4, AccountPassword = "correctPass" };
            repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(acc);

            var result = await svc.ChangePasswordAsync(4, "wrongPass", "newPass");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Current password is incorrect");
        }

        [Fact]
        public async Task ChangePasswordAsync_AccountNotFound_ReturnsFailure()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SystemAccount?)null);

            var result = await svc.ChangePasswordAsync(999, "any", "new");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Account not found");
        }

        [Fact]
        public async Task ChangePasswordAsync_HashPlaceholder_AllowsChange()
        {
            var (svc, repo) = Build();
            var acc         = new SystemAccount
            {
                AccountID       = 5,
                AccountPassword = "@@abc123@@_HASH_PLACEHOLDER"
            };
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(acc);
            repo.Setup(r => r.UpdateAsync(It.IsAny<SystemAccount>())).Returns(Task.CompletedTask);

            var result = await svc.ChangePasswordAsync(5, "anyPassword", "newSecurePass");

            result.Success.Should().BeTrue();
        }
    }
}
