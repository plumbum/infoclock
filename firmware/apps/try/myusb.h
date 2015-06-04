#ifndef _MYUSB_H_
#define _MYUSB_H_

#include "ch.h"
#include "hal.h"

extern SerialUSBDriver SDU1;

void myusbInit(void);

usbstate_t myusbState(void);

#endif /* _MYUSB_H_ */

