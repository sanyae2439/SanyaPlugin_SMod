# SanyaPlugin
SCP SL Plugin <JP Plugin>
(自分のサーバー用のため、他サーバーので動作は保証できません。)

Smod2が必要です
https://github.com/Grover-c13/Smod2

# Contact
Discord : 初音早猫#0001

# Download

https://github.com/hatsunemiku24/SanyaPlugin/tree/master/SanyaPlugin/Archives

最新のコミットに合わせたものをダウンロードしてください

# Translation

https://github.com/hatsunemiku24/SanyaPlugin/tree/master/SanyaPlugin/Translations

SanyaPlugin_ja.txtをsm_translationsに入れると日本語になります

# Install
「sm_plugins」に入れるだけ

# Config
## システム系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_info_sender_to_ip | String | hatsunemiku24.ddo.jp | サーバー情報送信先IP
sanya_info_sender_to_port | String | 37813 | サーバー情報送信先ポート
sanya_steam_kick_limited | Bool | False | Steamの制限付きアカウントをキックします
sanya_motd_enabled | Bool | False | MOTDの有効化（メッセージはTranslationに）
sanya_motd_target_role | String | Empty | 特定ロールは別のメッセージを表示
sanya_event_mode_weight | List<Int> | 1,-1,-1,-1,-1 | モードのランダム比率（通常/Night/実験中/D反乱/中層）-1で無効 すべて-1の場合は通常になります
sanya_classd_ins_items | Int | 10 | 反乱時のドロップ数 増やしすぎると重い
sanya_hczstart_mtf_and_ci | Int | 3 | 中層モード時のガードの数
sanya_title_timer | Bool | False | Nキーのプレイヤーリストにラウンド経過時間表示
sanya_cassie_subtitle | Bool | False | 放送に字幕を表示
sanya_friendly_warn | Bool | False | FFした人に警告を表示
sanya_friendly_warn_console | Bool | False | FFの被害者と加害者両方へ@キーコンソールへ表示（字幕と併用可能）
sanya_summary_less_mode | Bool | False | リザルト画面をなしにラウンドを終了する機能
sanya_endround_all_godmode | Bool | False | ラウンド終了時全員を無敵にする
sanya_nuke_start_countdown_door_lock | Bool | False | 核起動開始時に一部(SCP-106、ゲート、チェックポイント)を除きすべてのドアをオープンする
sanya_ci_and_scp_noend | Bool | False | CIとSCPだけが残ってもラウンドが終了しないようになる
sanya_first_respawn_time_fast | Float | 1.0 | 最初の増援時間に対する除数（2.0だと半分になる）
  
## データ&EXP
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_data_enabled | Bool | False | プレイヤーデータのDBを作成する
sanya_data_global | Bool | False | 複数サーバー共通のDBを使うか
sanya_level_enabled | Bool | False | Badge欄にLevelを表示
sanya_level_exp_kill | Int | 3 | キル時の経験値
sanya_level_exp_death | Int | 1 | デス時の経験値
sanya_level_exp_win | Int | 10 | 勝利時の経験値
sanya_level_exp_other | Int | 3 | 勝利以外時の経験値

## ユーザーコマンド
 \`キーで開くコンソールで使用するものです
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_user_command_enabled | Bool | False | ユーザーコマンドの有効化
sanya_user_command_cooltime | Int | 20 | ユーザーコマンドを使用できる間隔（変更しないほうがいいでしょう）
sanya_user_command_enabled_kill | Bool | True | .killコマンドの有効化
sanya_user_command_enabled_sinfo | Bool | True | .sinfoコマンドの有効化
sanya_user_command_enabled_079nuke | Bool | True | .079nukeコマンドの有効化
sanya_user_command_enabled_939sp | Bool | True | .939spコマンドの有効化
sanya_user_command_enabled_079sp | Bool | True | .079spコマンドの有効化
sanya_user_command_enabled_radio | Bool | True | .radioコマンドの有効化
sanya_user_command_enabled_boost | Bool | True | .boostコマンドの有効化
sanya_user_command_enabled_attack | Bool | True | .attackコマンドの有効化

## SCP系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_generator_engaged_cantopen | Bool | False | 発電機が起動完了した場合に開かないように
sanya_scp079_lone_boost | Bool | False | 079が最後のSCPになった際に発電機自由解放&Tier5に
sanya_scp079_all_flick_light_tier | Int | -1 | 079がロックダウン時全館停電を起こせるTier
sanya_scp079_speaker_no_ap_use | Bool | False | 079がスピーカー使用時に電力を使わなくなる
sanya_scp914_changing | Bool | False | SCP-914に入った人の扱いを少し変更
sanya_scp939_dot_damage | Int | -1 | SCP-939に出血ダメージを付与
sanya_scp939_dot_damage_total | Int | 80 | 出血ダメージの総量
sanya_scp939_dot_damage_interval | Int(Second) | 1 | 出血ダメージの間隔
sanya_scp106_portal_to_human_wait　| Int | 180 | SCP-106ポータルの初回使用可能までの時間
sanya_scp106_lure_speaktime | Int | -1 | SCP-106の囮コンテナに入った際一定時間死なずに放送可能に
sanya_scp106_hitmark_pocket_death | Bool | False | ポケットディメンション内で人が死ぬと106にヒットマークが出るように
sanya_scp096_damage_trigger | Bool | False | 096がダメージを受けると発狂トリガー
sanya_scp106_cleanup | Bool | False | Smodのscp106_cleanupが動かないときに使う用
sanya_infect_by_scp049_2 | Bool | False | SCP-049-2がキルした死体をSCP-049が治療可能に
sanya_infect_limit_time | Int | 4 | SCP-049が治療できなくなるまでの時間

## 人間系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_handcuffed_cantopen | Bool | False | 被拘束時にドアとエレベーターの操作を不能に
sanya_medkit_stop_dot_damage | Bool | False | 939の出血などを医療キットで止められるように
sanya_grenade_hitmark | Bool | False | グレネード命中時投げた人にヒットマークが出るように

## 独自要素
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_scp_disconnect_at_resetrole | Bool | False | SCPで切断された場合元の状態へ復帰
sanya_suicide_need_weapon | Bool | False | .killコマンド時に武器を持つ必要があるか
sanya_original_auto_nuke | Bool | False | 独自判定の自動核を設定
sanya_nuke_button_auto_close | Float | -1f | 核起動ボタンの蓋が自動で閉まる時間 & 核起動室の扉をEXIT_ACC持ちで開けられるように (-1で無効)
sanya_stop_mtf_after_nuke | Bool | False | 核起爆後の増援停止
sanya_lock_surface_gate_before_countdown | Bool | False | 核カウントダウン開始までは地上A-Bゲートが開かないように
sanya_inventory_card_act | Bool | False | カードキーがインベントリ内でも効果発揮
sanya_escape_spawn | Bool | False | NTFに転生する際の場所を変更
sanya_intercom_information | Bool | False | 放送室のモニターに生存者情報を表示＆放送室のキーカードが不要に
sanya_traitor_limitter | Int | -1 | 被拘束状態でNTF/カオスが脱出ポイントへ行くと敵対勢力に寝返ることができる。その際の寝返り元生存人数がこの値以下でないとできない
sanya_traitor_chance_percent | Int | 50 | 寝返り成功率

## ダメージ調整系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_fallen_limit | Float | 10.0 | この値以下の落下ダメージを無効にする
sanya_usp_damage_multiplier_human | Float | 2.5 | USPのダメージ倍率（対人間）
sanya_usp_damage_multiplier_scp | Float | 5.0 | USPのダメージ倍率（対SCP）
sanya_damage_divisor_cuffed | Float | 1.0 | 被拘束時に受けるダメージ除数
sanya_damage_divisor_scp173 | Float | 1.0 | SCP-173が受けるダメージ除数
sanya_damage_divisor_scp106 | Float | 1.0 | SCP-106が受けるダメージ除数
sanya_damage_divisor_scp106_grenade | Float | 1.0 | SCP-106が受けるグレネードのダメージ
sanya_damage_divisor_scp049 | Float | 1.0 | SCP-049が受けるダメージ除数
sanya_damage_divisor_scp049_2 | Float | 1.0 | SCP-049-2が受けるダメージ除数
sanya_damage_divisor_scp096 | Float | 1.0 | SCP-096が受けるダメージ除数
sanya_damage_divisor_scp939 | Float | 1.0 | SCP-939が受けるダメージ除数

## 回復系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_recovery_amount_scp173 | Int | -1 | SCP-173がキル時に回復する量
sanya_recovery_amount_scp106 | Int | -1 | SCP-106がディメンションから人間が脱出できなかった際に回復する量
sanya_recovery_amount_scp049 | Int | -1 | SCP-049が治療成功時に回復する量
sanya_recovery_amount_scp049_2 | Int | -1 | SCP-049-2がキル時に回復する量
sanya_recovery_amount_scp096 | Int | -1 | SCP-096がキル時に回復する量
sanya_recovery_amount_scp939 | Int | -1 | SCP-939がキル時に回復する量

## DefaultAmmo
値は 5mm,7mm,9mm の順です

設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_default_ammo_classd | List<int> | 15,15,15 | クラスDが初期で所持する弾数
sanya_default_ammo_scientist | List<int> | 20,15,15 | 科学者が初期で所持する弾数
sanya_default_ammo_guard | List<int> | 0,35,0 | 施設警備員が初期で所持する弾数
sanya_default_ammo_ci | List<int> | 0,200,20 | カオスが初期で所持する弾数
sanya_default_ammo_ntfscientist | List<int> | 80,40,40 | NTF Scientistが初期で所持する弾数
sanya_default_ammo_cadet | List<int> | 10,10,80 | NTF Cadetが初期で所持する弾数
sanya_default_ammo_lieutenant | List<int> | 80,40,40 | NTF Lieutenantが初期で所持する弾数
sanya_default_ammo_commander | List<int> | 130,50,50 | NTF Commanderが初期で所持する弾数
