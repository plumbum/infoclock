forgetram \ Clean RAM  words

\ Display ID
$8989 constant LCD-ID \ Controller: SSD1289 / Display: TFT8K1711FPC-A1-E / Board: YX32B

\ Geometry
320 constant lcd_width
240 constant lcd_height

\ Pins and port definite
PA8  constant lcd_bl
PC13 constant lcd_cs
PA0  constant lcd_rs
PA1  constant lcd_wr
PA2  constant lcd_rd
PB0  constant lcd_d0
lcd_d0 io-base constant lcd_port_base
lcd_port_base GPIO.ODR + constant lcd_port_odr
lcd_port_base GPIO.IDR + constant lcd_port_idr

: lcd_cs_on lcd_cs ioc! ;
: lcd_cs_off lcd_cs ios! ;

\ Read whole port
: (lcd_read) ( -- data )
    lcd_rd ioc!
    lcd_port_idr @
    lcd_rd ios!
;

\ Write whole port
: (lcd_write) ( data -- )
    lcd_port_odr ! lcd_wr ioc! lcd_wr ios!
;

: (lcd_port_read_mode) ( -- )
    $FFFF lcd_port_odr !
    IMODE-PULL lcd_d0 $FFFF io-modes!
;

: (lcd_port_write_mode) ( -- )
    OMODE-PP OMODE-FAST + lcd_d0 $FFFF io-modes!
;

\ Read device code word
: lcd_read_id ( -- device_code )
    (lcd_port_read_mode)
    lcd_rs ioc! \ Status register
    nop
    (lcd_read)
;

\ Write register
: lcd_reg_write ( data idx -- )
    (lcd_port_write_mode)
    lcd_rs ioc! \ Register index
    (lcd_write)
    lcd_rs ios! \ Data
    (lcd_write)
;

: lcd_mkcolor ( r g b -- color )
    $F8 and 3 rshift swap \ blue
    $FC and 3 lshift or swap \ green
    $F8 and 8 lshift or \ red
;

: lcd_area ( x1 y1 x2 y2 -- square )
    (lcd_port_write_mode)
    \ Horizontal RAM address position (R44h) 
    dup 8 lshift
    3 pick
    or $44 lcd_reg_write
    \ Vertical RAM address position (R45h-R46h) 
    over $46 lcd_reg_write
    3 pick $45 lcd_reg_write
    3 pick $4f lcd_reg_write \ set X
    2 pick $4e lcd_reg_write \ set Y
    \ Calculate square
    rot - 1+ \ Y subs
    -rot swap - 1+ \ X subs
    * \ Xs*Ys
;

: lcd_box ( x1 y1 x2 y2 color -- )
    >r
    lcd_area
    r> swap
    lcd_rs ioc! \ Index
    $22 (lcd_write) \ Write command
    lcd_rs ios! \ Data
    0 do
        \ dup (lcd_write) \ Write data
        \ Speed up
        dup lcd_port_odr ! lcd_wr ioc! lcd_wr ios!
    loop
    drop
;

: lcd_clear ( color -- )
    >r
    0 0 lcd_width 1- lcd_height 1-
    r>
    lcd_box
;

: lcd_pixel ( x y color -- )
    -rot
    $4e lcd_reg_write \ set Y
    $4f lcd_reg_write \ set X
    $22 lcd_reg_write \ write pixel
;

$FFFF variable lcd_fg_color
$0000 variable lcd_bg_color

: lcd_bigchar ( char x y -- )
    dup 29 + \ Height 30
    2 pick 15 + \ Width 16
    swap
    lcd_area
    swap
    32 - \ Ctrl chars skip
    64 * \ Char offset
    16 + \ Skip header
    font16x30 +

    \ Entry mode setting (R11h)
    \ R11H Entry mode
    \ vsmode DFM1 DFM0 TRANS OEDef WMode DMode1 DMode0 TY1 TY0 ID1 ID0 AM LG2 LG2 LG0
    \   0     1    1     0     0     0     0      0     0   1   1   1  *   0   0   0
    \ Temporary change write directon
    $6070 $11 lcd_reg_write

    lcd_rs ioc! \ Index
    $22 (lcd_write) \ Write command
    lcd_rs ios! \ Data
    swap 0 do
        31 1 do
            i $1F xor bit over bit@
            if lcd_fg_color else lcd_bg_color then
            \ @ (lcd_write) \ Write data
            \ Speed up
            @ lcd_port_odr ! lcd_wr ioc! lcd_wr ios!
        loop
        4 +
    30 +loop
    drop
    \ Recovery write direction
    $6078 $11 lcd_reg_write \ 0x6070
;


: setcolor ( color -- ) lcd_fg_color ! ;

: setbgcolor ( color -- ) lcd_bg_color ! ;

: putpixel ( x y -- ) lcd_fg_color @ lcd_pixel ;

: clear ( -- ) lcd_bg_color @ lcd_clear ;

: display ( -- ) ; \ Does nothing

: showdigit ( n x y -- )
    rot 256 * digits + 
    (lcd_port_write_mode)
    64 0 do
        ( x y ptr )
        2 pick $4f lcd_reg_write \ set X
        over i + $4e lcd_reg_write \ set Y
        lcd_rs ioc! \ Command register
        $22 (lcd_write)
        lcd_rs ios! \ Data register
        32 0 do
            i $1F xor bit over bit@
            if lcd_fg_color else lcd_bg_color then
            \ @ (lcd_write) \ Write data
            \ Speed up
            @ lcd_port_odr ! lcd_wr ioc! lcd_wr ios!
        loop
        4 +
    loop
    2drop drop
;

: shownum ( u -- )
    10 /mod 10 /mod 10 /mod
\   X  Y
    0  0 showdigit
    32 0 showdigit
    64 0 showdigit
    96 0 showdigit
;

: test
    lcd_cs_on
    millis
    100 0 do i shownum loop
    millis swap - cr . cr
    lcd_cs_off
;

: (lcd_init_pins) ( -- )
    lcd_bl ioc! OMODE-PP OMODE-FAST + lcd_bl io-mode!
    lcd_cs ios! OMODE-PP OMODE-FAST + lcd_cs io-mode!
    lcd_wr ios! OMODE-PP OMODE-FAST + lcd_wr io-mode!
    lcd_rd ios! OMODE-PP OMODE-FAST + lcd_rd io-mode!
    lcd_rs ioc! OMODE-PP OMODE-FAST + lcd_rs io-mode!
    $FFFF (lcd_write)
    (lcd_port_read_mode)
;

: lcd_init ( -- )
    (lcd_init_pins)

    lcd_cs_on
    \ power supply setting
    \ set R07h at 0021h (GON=1,DTE=0,D[1:0]=01)
    $0021 $07 lcd_reg_write
    \ set R00h at 0001h (OSCEN=1)
    $0001 $00 lcd_reg_write
    \ set R07h at 0023h (GON=1,DTE=0,D[1:0]=11)
    $0023 $07 lcd_reg_write
    \ set R10h at 0000h (Exit sleep mode)
    $0000 $10 lcd_reg_write
    \ Wait 30ms
    30 ms
    \ set R07h at 0033h (GON=1,DTE=1,D[1:0]=11)
    $0033 $07 lcd_reg_write
    \ Entry mode setting (R11h)
    \ R11H Entry mode
    \ vsmode DFM1 DFM0 TRANS OEDef WMode DMode1 DMode0 TY1 TY0 ID1 ID0 AM LG2 LG2 LG0
    \   0     1    1     0     0     0     0      0     0   1   1   1  *   0   0   0
    $6078 $11 lcd_reg_write \ 0x6070
    \ LCD driver AC setting (R02h)
    $0600 $02 lcd_reg_write
    \ power control 1
    \ DCT3 DCT2 DCT1 DCT0 BT2 BT1 BT0 0 DC3 DC2 DC1 DC0 AP2 AP1 AP0 0
    \ 1     0    1    0    1   0   0  0  1   0   1   0   0   1   0  0
    \ DCT[3:0] fosc/4 BT[2:0]  DC{3:0] fosc/4
    $0804 $03 lcd_reg_write \ 0xA8A4
    $0000 $0C lcd_reg_write
    $0808 $0D lcd_reg_write \ 0x080C --> 0x0808
    \ power control 4
    \ 0 0 VCOMG VDV4 VDV3 VDV2 VDV1 VDV0 0 0 0 0 0 0 0 0
    \ 0 0   1    0    1    0    1    1   0 0 0 0 0 0 0 0
    $2900 $0E lcd_reg_write
    $00B8 $1E lcd_reg_write
    $293F $01 lcd_reg_write \ 0x2B3F); // Driver output control 320*240  0x6B3F
    $0000 $10 lcd_reg_write
    $0000 $05 lcd_reg_write
    $0000 $06 lcd_reg_write
    $EF1C $16 lcd_reg_write
    $0003 $17 lcd_reg_write
    $0233 $07 lcd_reg_write \ 0x0233
    %11 6 lshift $0B lcd_reg_write
    $0000 $0F lcd_reg_write \ Gate scan start position
    $0000 $41 lcd_reg_write
    $0000 $42 lcd_reg_write
    $0000 $48 lcd_reg_write
    $013F $49 lcd_reg_write
    $0000 $4A lcd_reg_write
    $0000 $4B lcd_reg_write
    $EF00 $44 lcd_reg_write
    $0000 $45 lcd_reg_write
    $013F $46 lcd_reg_write
    $0707 $30 lcd_reg_write
    $0204 $31 lcd_reg_write
    $0204 $32 lcd_reg_write
    $0502 $33 lcd_reg_write
    $0507 $34 lcd_reg_write
    $0204 $35 lcd_reg_write
    $0204 $36 lcd_reg_write
    $0502 $37 lcd_reg_write
    $0302 $3A lcd_reg_write
    $0302 $3B lcd_reg_write
    $0000 $23 lcd_reg_write
    $0000 $24 lcd_reg_write
    $8000 $25 lcd_reg_write   \ 65hz
    $0000 $4f lcd_reg_write \ Set GDDRAM X address counter 
    $0000 $4e lcd_reg_write \ Set GDDRAM Y address counter 

    lcd_bl ios! \ Enable backlight

    0 0 $66 lcd_mkcolor lcd_clear

    lcd_cs_off
;

\ lcd_bl ios!
\ 1000 ms
