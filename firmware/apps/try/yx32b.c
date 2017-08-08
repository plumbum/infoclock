#include "yx32b.h"
#include "fnt8x8.h"

inline static void dataOut(void)
{
    palSetPad(GPIOA, GPIOA_LCD_RD);
    palClearPad(GPIOA, GPIOA_LCD_WR);
    palSetGroupMode(GPIOB, 0xFFFF, 0, PAL_MODE_OUTPUT_PUSHPULL);
}

inline static void dataIn(void)
{
    palSetPad(GPIOA, GPIOA_LCD_WR);
    palClearPad(GPIOA, GPIOA_LCD_RD);
    palSetGroupMode(GPIOB, 0xFFFF, 0, PAL_MODE_INPUT_PULLUP);
}

inline static void setRS(bool data)
{
    if (data) {
        palSetPad(GPIOA, GPIOA_LCD_RS);
    } else {
        palClearPad(GPIOA, GPIOA_LCD_RS);
    }
}

inline static void writeLcd(lcd_data_t data)
{
    palWritePort(GPIOB, data);
    palClearPad(GPIOC, GPIOC_LCD_CS);
    // __asm__ volatile ("nop");
    palSetPad(GPIOC, GPIOC_LCD_CS);
}

inline static lcd_data_t readLcdOnce(void)
{
    dataIn();
    palClearPad(GPIOC, GPIOC_LCD_CS);
    // __asm__ volatile ("nop");
    lcd_data_t r = palReadPort(GPIOB);
    r = palReadPort(GPIOB); // Read two times for correct
    palSetPad(GPIOC, GPIOC_LCD_CS);
    dataOut();
    return r;
}

static void writeReg(lcd_data_t reg, lcd_data_t data)
{
    setRS(0);
    writeLcd(reg);
    setRS(1);
    writeLcd(data);
}

lcd_color_t lcdColor(uint8_t r, uint8_t g, uint8_t b)
{
    return LCD_COLOR(r, g, b);
}

void lcdFill(lcd_color_t color)
{
    writeReg(0x004f, 0); // Set GDDRAM X address counter 
    writeReg(0x004e, 0); // Set GDDRAM Y address counter 
    setRS(0); writeLcd(0x22); // RAM data write register
    setRS(1);
    for(int i=0; i<320*240; i++) {
        writeLcd(color);
    }
}

void lcdPixel(int x, int y, lcd_color_t color)
{
    writeReg(0x004f, x); // Set GDDRAM X address counter 
    writeReg(0x004e, y); // Set GDDRAM Y address counter 
    writeReg(0x0022, color);
}

#define FONT_W 8
#define FONT_H 8

void lcdChar(int x, int y, char c, lcd_color_t fg, lcd_color_t bg)
{
    uint8_t* ptr = fnt8x8 + (unsigned int)c*8;
    for(int py=y; py<y+FONT_H; py++) 
    {
        writeReg(0x004f, x); // Set GDDRAM X address counter 
        writeReg(0x004e, py); // Set GDDRAM Y address counter 
        setRS(0); writeLcd(0x22); // RAM data write register
        setRS(1);
        uint8_t l = *ptr++;
        for(int px=x; px<x+FONT_W; px++)
        {
            if (l & 0x80) {
                writeLcd(fg);
            } else {
                writeLcd(bg);
            }
            l <<= 1;
        }
    }
}

void lcdStr(int x, int y, char* s, lcd_color_t fg, lcd_color_t bg)
{
    char c;
    while((c = *s++) != 0) {
        lcdChar(x, y, c, fg, bg);
        x += FONT_W;
    }
}

int lcdInit(void)
{
    setRS(0);
    lcd_data_t status = readLcdOnce();
    if (status != 0x8989) {
        return (1<<31) | status; // unknow lcd
    }

    dataOut();

    // power supply setting
    // set R07h at 0021h (GON=1,DTE=0,D[1:0]=01)
    writeReg(0x0007,0x0021);
    // set R00h at 0001h (OSCEN=1)
    writeReg(0x0000,0x0001);
    // set R07h at 0023h (GON=1,DTE=0,D[1:0]=11)
    writeReg(0x0007,0x0023);
    // set R10h at 0000h (Exit sleep mode)
    writeReg(0x0010,0x0000);
    // Wait 30ms
    chThdSleepMilliseconds(30);
    // set R07h at 0033h (GON=1,DTE=1,D[1:0]=11)
    writeReg(0x0007,0x0033);
    // Entry mode setting (R11h)
    // R11H Entry mode
    // vsmode DFM1 DFM0 TRANS OEDef WMode DMode1 DMode0 TY1 TY0 ID1 ID0 AM LG2 LG2 LG0
    //   0     1    1     0     0     0     0      0     0   1   1   1  *   0   0   0
    writeReg(0x0011, 0x6078); // 0x6070
    // LCD driver AC setting (R02h)
    writeReg(0x0002,0x0600);
    // power control 1
    // DCT3 DCT2 DCT1 DCT0 BT2 BT1 BT0 0 DC3 DC2 DC1 DC0 AP2 AP1 AP0 0
    // 1     0    1    0    1   0   0  0  1   0   1   0   0   1   0  0
    // DCT[3:0] fosc/4 BT[2:0]  DC{3:0] fosc/4
    writeReg(0x0003,0x0804);//0xA8A4
    writeReg(0x000C,0x0000);//
    writeReg(0x000D,0x0808);// 0x080C --> 0x0808
    // power control 4
    // 0 0 VCOMG VDV4 VDV3 VDV2 VDV1 VDV0 0 0 0 0 0 0 0 0
    // 0 0   1    0    1    0    1    1   0 0 0 0 0 0 0 0
    writeReg(0x000E, 0x2900);
    writeReg(0x001E, 0x00B8);
    writeReg(0x0001, 0x293F); // 0x2B3F); // Driver output control 320*240  0x6B3F
    writeReg(0x0010, 0x0000);
    writeReg(0x0005, 0x0000);
    writeReg(0x0006, 0x0000);
    writeReg(0x0016, 0xEF1C);
    writeReg(0x0017, 0x0003);
    writeReg(0x0007, 0x0233); // 0x0233
    writeReg(0x000B, 0x0000|(3<<6));
    writeReg(0x000F, 0x0000); // Gate scan start position
    writeReg(0x0041, 0x0000);
    writeReg(0x0042, 0x0000);
    writeReg(0x0048, 0x0000);
    writeReg(0x0049, 0x013F);
    writeReg(0x004A, 0x0000);
    writeReg(0x004B, 0x0000);
    writeReg(0x0044, 0xEF00);
    writeReg(0x0045, 0x0000);
    writeReg(0x0046, 0x013F);
    writeReg(0x0030, 0x0707);
    writeReg(0x0031, 0x0204);
    writeReg(0x0032, 0x0204);
    writeReg(0x0033, 0x0502);
    writeReg(0x0034, 0x0507);
    writeReg(0x0035, 0x0204);
    writeReg(0x0036, 0x0204);
    writeReg(0x0037, 0x0502);
    writeReg(0x003A, 0x0302);
    writeReg(0x003B, 0x0302);
    writeReg(0x0023, 0x0000);
    writeReg(0x0024, 0x0000);
    writeReg(0x0025, 0x8000);   // 65hz
    writeReg(0x004f, 0); // Set GDDRAM X address counter 
    writeReg(0x004e, 0); // Set GDDRAM Y address counter 

    lcdFill(COLOR_BLACK);

    return 0;
}

void lcdTest(void)
{
    for(int y=0; y<240; y++) {
        lcdPixel(0, y, COLOR_WHITE);
        lcdPixel(319, y, COLOR_WHITE);
    }
    for(int x=0; x<320; x++) {
        lcdPixel(x, 0, COLOR_WHITE);
        lcdPixel(x, 239, COLOR_WHITE);
    }

    lcdStr(2, 2, "Hello world!", COLOR_GREEN, COLOR_BLUE);
    return;
    writeReg(0x004f, 0); // Set GDDRAM X address counter 
    writeReg(0x004e, 0); // Set GDDRAM Y address counter 
    setRS(0);
    writeLcd(0x22); // RAM data write register
    setRS(1);
    for(int y=0; y<240; y++) {
        for(int x=0; x<320; x++) {
            writeLcd(lcdColor(y, x, 0));
            /*
            if((y & 0x10) ^ (x & 0x10)) {
                writeLcd(lcdColor(0xff, 0, 0));
            } else {
                writeLcd(lcdColor(0, 0xff, 0));
            }
            */
        }
    }
}