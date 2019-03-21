# SanyaPlugin
SCP SL Plugin <JP Plugin>

Smod2が必要です
https://github.com/Grover-c13/Smod2

# Install
「sm_plugins」に入れるだけ

# Config
## システム系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
sanya_info_sender_to_ip | String | hatsunemiku24.ddo.jp | サーバー情報送信先IP
sanya_info_sender_to_port | String | 37813 | サーバー情報送信先ポート
sanya_spectator_slot | Int | 0 | 観戦スロットの数
sanya_event_mode_weight | List<Int> | 1,-1,-1,-1,-1 | モードのランダム比率（通常/Night/実験中/D反乱/中層）-1で無効 すべて-1の場合は通常になります
sanya_classd_ins_items | Int | 10 | 反乱時のドロップ数 増やしすぎると重い
sanya_night_mode | Bool | False | NightModeの有効化
sanya_title_timer | Bool | False | Nキーのプレイヤーリストにラウンド経過時間表示
sanya_cassie_subtitle | Bool | False | 放送に字幕を表示
sanya_friendly_warn | Bool | False | FFした人に警告を表示
sanya_summary_less_mode | Bool | False | リザルト画面をなしにラウンドを終了する機能
sanya_endround_all_godmode | Bool | False | ラウンド終了時全員を無敵にする

## SCP系
設定名 | 値の型 | 初期値 | 説明
--- | :---: | :---: | ---
generator_engaged_cantopen | Bool | False | 発電機が起動完了した場合に開かないように
sanya_scp914_changing | Bool | False | SCP-914に入った人の扱いを少し変更
sanya_scp106_portal_to_human | Bool | False | SCP-106が生存者の足元にポータルを作成可能に
sanya_scp106_portal_to_human_wait　| Int | 180 | SCP-106ポータルの初回使用可能までの時間
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
sanya_original_auto_nuke | Bool | False | 独自判定の自動核を設定
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
  
# History
- v1.0
さにゃぷらぐいん作成
脱出場所変更機能がついた

- v2.0
複製機能が付いた

- v3.0
scp106_cleanupがバグってるのでさにゃぷらぐいんに
死体お片付けを追加
- v3.1
複製できるSCPが自害系ダメージを食らわない不具合を修正
- v3.2
SMod 3.1.10でscp106_cleanupが直ったので機能削除

- v4.0
049の治療可能時間を設定可能に
049-2がキル時に049が治療可能に
- v4.1
106の複製機能を実装
（ただしポケットディメンションへ複製される）
（使うときはpd_exit_count: 8を推奨)

- v5.0
Alphawarhead起動時のドアを
開かない+ロックされないようにする機能を実装
- v5.1
ポケットディメンション片付けを復帰
- v5.2
コード整理 + コンフィグ簡略化

- v6.0
SCPごとに回復値/間隔を設定可能に

- v7.0
SCP-079をギミックとして実装

- v7.1
細かい動作修正

- v8.0
「裏切り」システム追加
PocketCleanerがたまにAPIエラーを起こすのを修正
- v8.1
「裏切り」の確立設定が変だったのを修正
- v8.2
「裏切り」の判定範囲が狭すぎたので拡大
Nキー上部に表示する文字を設定可能に
Nキー上部に試合時間カウンタを表示
- v8.3
079が下層閉鎖前には間隔が設定値の2倍になるように
- v8.4
みにぱっち
- v8.5
裏切りシステムの対象にコマンダーとルーテンも追加
- v8.6
Smod 3.1.19に対応
- v8.7
ペスト延長とPocketCleanerの内部動作変更
複製機能を削除(あんまり使わないため)

- v9.0
さにゃぼっととの連携実装
- v9.1
バグ修正
- v9.2
バグ修正

- v10.0-eXpi
5つの新機能追加
-v 10.1-eXpi
ドアロックの秒数設定可能に
ラウンド開始時Dクラス収容室にランダムドロップ
- v10.1.1-eXpi
SCP079にアナウンス追加
- v10.1.2-eXpi
さばりす送信のバグを修正
- v10.1.3-eXpi
ログがうるさいので修正
- v10.1.4-eXpi
回復のバグ修正

- v10.2
観戦専用枠追加機能を実装

- v10.3
タブレットロックにクールタイムを実装

- v10.4
SCP-079を廃止（本家版が出たため）
Nキータイマーを追加するかどうかに変更
ドアロックを拘束具に
SCPが待機ではなく行動で回復に（キルなど）
- v10.5
日本語字幕機能を実装

- v11.0
FF警告を追加
914ちぇんじを追加
指定落下ダメージ無効を追加
config読み込みを改善
自殺コマンドを追加（クライアント用）

- v12.0
SCP106のポータル仕様変更
乱数の改善
SCP仲間表示コマンド
発電機系に字幕追加
発電機をロック解除したら起動完了するまで閉まらないように
発電機ロック解除にO5?
起動までの時間短縮
起動完了したら開かなくなる
SCP以外は発電機を止められない
079すぴーかーの使用量をほぼ0に
106囮コンテナに入ると放送に
NightMode

- v12.1
939放送コマンド
- v12.1.1
generator_fixの機能がsmodに入ったため一部機能削除
generator_insert_teams: 1,2,3,4
generator_eject_teams: 0,2,4
で今まで通りの機能が得られます

- v12.2
コンフィグ調整
DefaultAmmo統合
ダメージ減算を追加
- v12.2.1
SM3.3.0-Aにリビルド
- v12.2.2
sanyaコマンドを拡充

- v12.3
新config書式
その他やることリスト追加
- v12.3.1
ばぐふぃっくす
- v12.3.2
核の動作変更
- v12.3.3
字幕の大きさ変更
こんふぃぐ変更

- v12.4
イベントモード実装
格闘/079射撃コマンド実装 
HP消費のブーストコマンド実装(173/096/939) 
コマンドにクールタイム実装
カードキーを手に持つ必要がなくなりました
079はロックされたドアを無視して操作できます
核爆発後は増援がわかないように
096が被ダメージで発狂トリガーオンに

- v12.4.1
中層モードを変更 
SCPがドアをあけられない時間を長くしました
Dクラス房の足元アイテムを復活
実験モード時にSCP-173/SCP-079が確定で沸きます
boostコマンドを改良
SCP-173のブースト仕様を変更
SCP-939のブースト仕様を変更

SCP-049のブースト機能を追加
コマンド受付間隔をやや短く
attackコマンドの当たり判定を改善
079のattackの威力をアップ
079start 079stopコマンドを削除
.079nukeで操作
079が放送を妨害可能に.079sp
