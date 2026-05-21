# CasinoApp unit tests

Run the unit tests from the solution folder:

```powershell
dotnet test .\CasinoApp.slnx
```

## Test files

- `GameRulesTests.cs` covers rule helpers for Roulette, Blackjack, Barbut, Mines, Slots, Scratch Card, Flip A Coin, and Wheel Of Fortune.
- `AccountRulesTests.cs` covers login field validation, registration field validation, and deposit/withdraw wallet validation.

The tests exercise pure rules in `CasinoApp.BusinessLogic` so UI pages can reuse the same calculations without database or browser setup.

## Coverage notes

Current unit tests cover:

- Roulette exact-number, zero, outside, dozen, and column payout multipliers.
- Blackjack ace hand values, blackjack payout, and push settlement.
- Barbut opponent comparison and returns.
- Mines multiplier and safe-cell win chance.
- Slots left-aligned payline matching and multiplier tiers.
- Scratch Card matching winning rows.
- Flip A Coin pot doubling and table limit threshold.
- Wheel Of Fortune winning-slot landing angle.
- Login and registration form validation rules.
- Deposit and withdrawal validation and balance math.

Useful next tests would be integration or UI tests for repository-backed flows:

- Duplicate username/email registration checks against a temporary database.
- Login success/failure with stored player credentials.
- Transaction creation during deposit and withdrawal.
- Component interaction tests for game buttons, animations, and JavaScript callbacks.
