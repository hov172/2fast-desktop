# Interactions

Motion, state changes and feedback. Short, because the app is deliberately quiet:
it is a security tool people open for four seconds at a time. Prefer no
animation over a decorative one.

## Motion vocabulary

| Duration | Where |
| --- | --- |
| `0:0:0.3` | `ImplicitOffset` — the `OffsetAnimation` applied to `TwoFASelectionListViewItemStyle`, so account rows slide rather than jump when the list re-orders, filters or a category changes |
| `0:0:0.240` | control-state transitions in `Styles/Generic/Button.xaml` and `SettingsCard.xaml` |
| `0:0:0.333` | WinUI-inherited control transitions |
| `0` | instantaneous visual-state setters — the majority |

Use the existing `ImplicitAnimationSet` resources rather than adding
`Storyboard`s to a view. New motion should reuse one of the durations above.

## Adaptive state

Layout changes are `VisualState` + `AdaptiveTrigger`, not code-behind. The shell
declares `NarrowState` (`MinWindowWidth="0"`) and `WideState`
(`MinWindowWidth="900"`) and only sets `ShellView.PaneDisplayMode`. Follow that
pattern: adaptive states set properties, they do not rebuild trees.

Mobile swaps the `NavigationView` for a `TabBar` via `ViewModel.IsMobile`.

## Loading and progress

- Long or indeterminate work uses `Shimmer` skeletons — placeholder shapes over
  the eventual layout, not a spinner over an empty page.
- `ImageEx` handles its own placeholder and fade for async images; do not
  hand-roll image loading.
- The TOTP countdown is a `RadialProgressBar` / `ProgressRing` driven by the
  period, and must stay smooth while the list is being filtered or scrolled.
- `IsLoading` on a view-model disables the triggering control for the duration.
  Commands must be idempotent under a double click — the existing login path
  guards with `if (IsLoading …) return;`.

## Feedback and errors

- Transient confirmations (code copied, and similar) use `AutoCloseTeachingTip`.
- Recoverable errors are inline and specific to the field. A failed unlock clears
  the password box and shows the error in place; it does not navigate away.
- Blocking errors and confirmations use a `ContentDialog` registered through
  `RegisterDialog` and shown via `IDialogService`. Do not construct dialogs ad hoc
  from a view-model — `MacOSSession.Message` does, and that is debt, not a
  pattern (see [architecture.md](architecture.md)).
- Error text never contains a secret, a password, or a vault path.

## Lock and session behaviour

Locking is not a visual state — it is enforced at the navigation layer.
`NavigationIsAllowed` goes false, in-flight operations are cancelled through the
session `CancellationToken`, the session secret is cleared and decrypted accounts
are dropped. Any new long-running operation must observe that token, or a lock
will leave work running against a vault the user believes is closed.

Codes are hidden by default when *Prefer hidden TOTP* is on and revealed per
entry on demand; revealing one entry must not reveal the rest.

## Input

- Every action reachable by pointer must be reachable by keyboard. Account-card
  actions (copy, edit, show QR) are visible controls precisely so they are
  focusable — do not move them behind hover.
- Search filters as you type against account name and service.
- Touch targets stay at or above `CardActionControlMinWidth` on cards; the shell
  pane uses `CompactPaneLength="56"`.
