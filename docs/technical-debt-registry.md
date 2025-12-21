# Technical Debt Registry

## High Priority Debt
| ID | Location | Description | Impact | Effort |
|----|----------|-------------|--------|--------|
| **DEBT-01** | `StoryProtocolClient.cs` | Missing actual ABI and contract addresses. Relying on placeholders. | Blocks IP Assets feature | L |
| **DEBT-02** | `ScenarioGraphValidator.cs` | RESOLVED: Implemented graph validator for cycles and reachability. | N/A | S |
| **DEBT-03** | `InMemoryStoreService.cs` | RESOLVED: Persistence implemented via localStorage. | N/A | S |
| **DEBT-09** | `GetBundleIpStatusQueryHandler.cs` | RESOLVED: Explorer URLs moved to configuration. | N/A | S |

## Medium Priority Debt
| ID | Location | Description | Impact | Effort |
|----|----------|-------------|--------|--------|
| **DEBT-04** | `WhatsAppBotService.cs` | RESOLVED: Support for template parameters implemented. | N/A | S |
| **DEBT-05** | `CheckAchievementsUseCase.cs` | RESOLVED: Decoupled hardcoded badge logic via repository. | N/A | S |
| **DEBT-06** | `HeroSection.razor` | RESOLVED: Legacy commented-out UI components removed. | N/A | S |

## Low Priority Debt
| ID | Location | Description | Impact | Effort |
|----|----------|-------------|--------|--------|
| **DEBT-07** | `YamlScenario.cs` | RESOLVED: Refactored mapping logic and fallbacks. | N/A | S |
| **DEBT-08** | Handlers | RESOLVED: Verified all Story Protocol URLs use configuration. | N/A | S |
