# SanyaPlugin
SCP SL Plugin <JP Plugin>

Smod2が必要です
https://github.com/Grover-c13/Smod2

# Config
## システム系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_info_sender_to_ip | String | hatsunemiku24.ddo.jp | サーバー情報送信先IP
sanya_info_sender_to_port | String | 37813 | サーバー情報送信先ポート
sanya_spectator_slot | Int | 0 | 観戦スロットの数
sanya_night_mode | Bool | False | NightModeの有効化
sanya_title_timer | Bool | False | Nキーのプレイヤーリストにラウンド経過時間表示
sanya_cassie_subtitle | Bool | False | 放送に字幕を表示
sanya_friendly_warn | Bool | False | FFした人に警告を表示

## SCP系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_generators_fix | Bool | False | 発電機の挙動を少し変更
sanya_scp914_changing | Bool | False | SCP-914に入った人の扱いを少し変更
sanya_scp106_portal_to_human | Bool | False | SCP-106が生存者の足元にポータルを作成可能に
sanya_scp106_lure_speaktime | Int | -1 | SCP-106の囮コンテナに入った際一定時間死なずに放送可能に
sanya_scp106_cleanup | Bool | False | Smodのscp106_cleanupが動かないときに使う用
sanya_infect_by_scp049_2 | Bool | False | SCP-049-2がキルした死体をSCP-049が治療可能に
sanya_infect_limit_time | Int | 4 | SCP-049が治療できなくなるまでの時間

## 人間系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_handcuffed_cantopen | Bool | False | 被拘束時にドアとエレベーターの操作を不能に
sanya_radio_enhance | Bool | False | RadioのRangeをURにした際に放送が可能に

## 独自要素
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
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
sanya_damage_divisor_scp173 | Float | 1.0 | SCP-173が受けるダメージ除数
sanya_damage_divisor_scp106 | Float | 1.0 | SCP-106が受けるダメージ除数
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
  
