# T66 — Kiểm thử 17 tình huống Phụ lục A

Ngày chạy: **24/10/2026** · Unity 6000.3.24f1 · `-batchmode -runTests`
Kết quả toàn bộ: **EditMode 134/134 đạt · PlayMode 67/68 đạt, 1 bỏ qua** (`Menu_MouseInputAndEscapePause…` cần Game view có focus, chạy trong Test Runner của Editor).

Mỗi dòng của Phụ lục A ([business-rules.md](business-rules.md)) được gắn với test tự động chứng minh nó. Cột "Cách kiểm" ghi test (tên phương thức) hoặc bước chơi tay khi không tự động hóa được.

| # | Tình huống | Quy tắc | Cách kiểm | Kết quả |
|---|---|---|---|---|
| 1 | Sai mọi câu, pháo trượt, cần phá nhiều lô cốt/xe tăng | BR-17, BR-30 | `SatchelCharge_CountAndReplenish`, `Phone_Wrong_Misses`, `Health_Bunker_ImmuneToBullet_TakesExplosive`, `Health_Tank_ImmuneToBullet_DiesFromAntiTank`. Vật tư không qua câu hỏi: Màn 1 có bộc phá ×2 cạnh lô cốt + tiếp tế khẩn cấp, Màn 2 có Bazooka cạnh xe tăng; Màn 3–5 chỉ có bộ binh và ổ súng máy, diệt được bằng súng mang theo. `Mission_RunsWithAllEnemiesActive_NoErrors` chạy cả 5 màn | Đạt |
| 2 | Hết hoặc đánh rơi vật tư nơi không tới được | BR-17 | `SatchelCharge_CountAndReplenish` (điểm cấp bổ sung nạp lại bộc phá); `EmergencyResupply` ở Màn 1 (màn duy nhất cần bộc phá) | Đạt |
| 3 | Câu hầm cấp 2 đúng sau thời gian dài | BR-06, BR-25, BR-34 | `Score_BR25` (câu hầm = cấp × 100, không thưởng tốc độ), `Resolve_10_20_30_OnlyCorrect_OncePerProfile`, `DeployWithoutBriefing_CorrectAnswersPayResolveOnceOnWin` | Đạt |
| 4 | Ba hòm sai, xen điện thoại đúng, rồi hòm hiếm | BR-16 | `Pity_ThreeWrongCrates_NextIsPity_ThenResets`, `InsuranceTracker_ActivatesOnlyAfterThreeWrong`, `Rarity_CommonUsesLevels1To2` | Đạt |
| 5 | Hết giờ tại hòm hoặc súng rơi | BR-11, BR-14 | `Crate_FreezesWorld_TimesOut_ThenResumesTogether`, `Score_WrongAndTimeoutGiveZero`, `CorrectQuizAndTimeout_ShowTruthfulVisibleFeedback` | Đạt |
| 6 | Thả E ở 0,9 s hoặc chết trước 1 s | BR-08 | `Hold_ReleasedAt09s_DoesNotOpen`, `Hold_FullSecond_OpensOnce_ThenNeedsRelease`, `Hold_ChangingTarget_Restarts` | Đạt |
| 7 | 0,5 s chuyển tiếp sau câu hỏi | BR-12 | `Crate_FreezesWorld_TimesOut_ThenResumesTogether`, `PauseAndQuizFreeze_StopWorldAudio_ButAllowUI_AndResume` | Đạt |
| 8 | Thiếu câu sau khi lọc | BR-28, BR-31 | `Selector_NeverRepeats_ThenRunsOut`, `Shortage_BlocksDeployment`, `Validate_Budget_ReportsShortfall`, `SharedSelector_FieldNeverRepeatsBunkerQuestions` | Đạt |
| 9 | Câu 121 ký tự; câu nháp có nguồn | BR-28 | `Validate_Field121Chars_AndDraft_AreNotShippable`, `Selector_FiltersDraft_Mission_Form_Level`; bản nộp build ngày 24/10 chạy Validate trước: **0 lỗi** | Đạt |
| 10 | Bằng ba chỉ số đầu, sát thương khác | BR-24 | `TH10_LessDamageWinsEvenWhenSlower`, `Combat_EachCriterionHasPriorityOverAllFollowingOnes` | Đạt |
| 11 | Bằng mọi tiêu chí, gửi khác thời điểm | BR-24, BR-25 | `TH11_TiesUseCompetitionRanksAndStableIds_RegardlessOfInputOrder`, `Knowledge_TiesIgnoreCombatAndTime` | Đạt |
| 12 | Đúng trong hầm rồi thoát / chết rồi chơi lại | BR-33, BR-34 | `LosingRun_PaysNoResolve`, `Take_OnlyForSameMission_AndOnlyOnce`, `Tutorial_HudTracksDamageAmmoStamina_ResultsAndFreshReplay` | Đạt |
| 13 | Thắng, khởi động lại, gửi lại nhiều lần | BR-34, BR-37, BR-39 | `Attempt_SettlesOnlyOnce_AndHasFreshIds`, `WinningRun_RankingWriteFailureCanRetryWithoutDuplicating`, `Store_IndependentBests_EqualEarlierRetained_ReloadAndDuplicateSafe`, `Enqueue_SurvivesRestart_AndIgnoresDuplicates` | Đạt |
| 14 | Túi đủ hai súng hoặc đạn đầy | BR-36 | `Inventory_ThirdWeapon_ReplacesCurrentAndDropsIt`, `Inventory_DuplicateWeapon_BecomesAmmoWithLeftover`, `Inventory_TakeBack_CreatesNoNewAmmo`, `Inventory_GrenadesOverCapacity_StayBehind` | Đạt |
| 15 | Điện thoại có mục tiêu đã bị phá | BR-38 | `Phone_Correct_DestroysTarget_AfterWorldResumes` (mục tiêu đã chết thì điện thoại khóa) | Đạt |
| 16 | Mất mạng sau khi máy chủ nhận | BR-39 | `Enqueue_SurvivesRestart_AndIgnoresDuplicates`, `Failures_BackOff_5_15_60_300_ThenStayAt300`, `Sent_RemovesEntry_AndSurvivesRestart`; máy gửi kèm `Idempotency-Key` = mã lượt, coi HTTP 409 là đã nhận | Đạt phía máy khách · máy chủ chưa triển khai |
| 17 | Đóng ứng dụng khi đang trả lời/quyết toán | BR-37 | `Profiles_BackupRecoveryAndInterruptedTemporaryFile`, `Profiles_SaveFailureRollsBackPurchase`, `FinalMissionSave_WritesBeforeAnnouncingSuccess` | Đạt |

## Ghi chú

- Dòng 16: máy chủ chấm lại (BR-26) nằm ngoài phạm vi bản nộp. Hàng chờ trên máy đã sẵn sàng; chỉ cần thêm `StreamingAssets/content/online.json` với `{"endpoint": "https://…"}` khi có server.
- Chơi tay cần làm thêm trước buổi demo (T37, T68): 3–5 người ngoài nhóm chơi hết Màn 1 và ghi góp ý vào bảng phản hồi.
