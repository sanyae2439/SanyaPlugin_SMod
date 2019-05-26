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

# Install
「sm_plugins」に入れるだけ

# Config
## システム系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_info_sender_to_ip | String | hatsunemiku24.ddo.jp | サーバー情報送信先IP
sanya_info_sender_to_port | String | 37813 | サーバー情報送信先ポート
sanya_motd_message | String | Empty | ログイン時のメッセージ（$nameは名前に置き換えられる）
sanya_motd_target_role | String | Empty | 特定ロールは別のメッセージを表示
sanya_motd_target_message | String | Empty | 特定ロールへのメッセージ
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
  
## データ&EXP
sanya_data_enabled | Bool | False | プレイヤーデータのDBを作成する
sanya_data_global | Bool | False | 複数サーバー共通のDBを使うか
sanya_level_enabled | Bool | False | Badge欄にLevelを表示

## SCP系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_generator_engaged_cantopen | Bool | False | 発電機が起動完了した場合に開かないように
sanya_scp079_lone_boost | Bool | False | 079が最後のSCPになった際に発電機自由解放&Tier5に
sanya_scp914_changing | Bool | False | SCP-914に入った人の扱いを少し変更
sanya_scp106_portal_to_human_wait　| Int | 180 | SCP-106ポータルの初回使用可能までの時間
sanya_scp106_lure_speaktime | Int | -1 | SCP-106の囮コンテナに入った際一定時間死なずに放送可能に
sanya_scp096_damage_trigger | Bool | False | 096がダメージを受けると発狂トリガー
sanya_scp106_cleanup | Bool | False | Smodのscp106_cleanupが動かないときに使う用
sanya_infect_by_scp049_2 | Bool | False | SCP-049-2がキルした死体をSCP-049が治療可能に
sanya_infect_limit_time | Int | 4 | SCP-049が治療できなくなるまでの時間

## 人間系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_handcuffed_cantopen | Bool | False | 被拘束時にドアとエレベーターの操作を不能に

## 独自要素
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_scp_disconnect_at_resetrole | Bool | False | SCPで切断された場合元の状態へ復帰
sanya_suicide_need_weapon | Bool | False | .killコマンド時に武器を持つ必要があるか
sanya_original_auto_nuke | Bool | False | 独自判定の自動核を設定
sanya_nuke_button_auto_close | Float | -1f | 核起動ボタンの蓋が自動で閉まる時間 & 核起動室の扉をEXIT_ACC持ちで開けられるように (-1で無効)
sanya_stop_mtf_after_nuke | Bool | False | 核起爆後の増援停止
sanya_inventory_card_act | Bool | False | カードキーがインベントリ内でも効果発揮
sanya_escape_spawn | Bool | False | NTFに転生する際の場所を変更
sanya_intercom_information | Bool | False | 放送室のモニターに生存者情報を表示＆放送室のキーカードが不要に
sanya_traitor_limitter | Int | -1 | 被拘束状態でNTF/カオスが脱出ポイントへ行くと敵対勢力に寝返ることができる。その際の寝返り元生存人数がこの値以下でないとできない
sanya_traitor_chance_percent | Int | 50 | 寝返り成功率
sanya_classd_startitem_percent | Int | -1 | クラスD収容房にアイテムを設置する(OK/NGのうちOKになる確率)
sanya_classd_startitem_ok_itemid | ItemType | 0 | OKの際に落とすアイテム
sanya_classd_startitem_no_itemid | ItemType | -1 | NGの際に落とすアイテム
sanya_doorlock_itemid | ItemType | -1 | このアイテムを持っている隊長はドアを操作時ロックできる
sanya_doorlock_locked_second | Int | 10 | ロックされ続ける時間
sanya_doorlock_interval_second | Int | 60 | 次にロックできるまでの時間

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
