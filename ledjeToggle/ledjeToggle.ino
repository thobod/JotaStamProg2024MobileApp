#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ArduinoJson.h>
#include <AsyncTimer.h>
#include <Wire.h>               // Only needed for Arduino 1.6.5 and earlier
#include "SSD1306Wire.h"        // legacy: #include "SSD1306.h"

enum Mode { CONNECT, CUT, CUTMESSAGE, DISARMED };

SSD1306Wire display(0x3c, D7, D6);
ESP8266WebServer server(80);
AsyncTimer  timer;

//char SSID[] = "TMNL-EEA6F1";
//char PWD[] = "LMS7LS3JLAFH7ED6";

char SSID[] = "Thomas";
char PWD[] = "Prol19261";

//correct connections = AE BG CH DF


// Define pin numbers for connections
const int pinA = D1;   // was d3
const int pinB = D2;   //     d4
const int pinC = D5;   //     d8
const int pinD = A0;   //     d0

const int pinE = D3;  //
const int pinF = D0;  //
const int pinG = D4;  // 
const int pinH = D8;  //

// Define pin for the LED
const int oledSCKPin = D6; // D8: LED pin
const int oledPinSDAPin = D7; // D8: LED pin

// Variables to track current connection state
bool isAEConnected = false;
bool isBGConnected = false;
bool isCHConnected = false;
bool isDFConnected = false;

// Variables to track previous connection state for debugging
bool wasAEConnected = false;
bool wasBGConnected = false;
bool wasCHConnected = false;
bool wasDFConnected = false;

//sound
bool _soundOn = false;
bool _beeping = false;
int _freq = 800;
int _duration = 300;
int _offDuration = 700;
bool _startedBeep = false;
int _volume = 1;
int _connectVolume = 1;
int _cutVolume = 15;
int _cutFailedVolume = 100;
int _cutSuccessVolume = 1;

int _cutMessageDuration = 5000;

// game state
Mode currentMode = CONNECT;
int totalTimeLimit = 3600000;
int remainingExplosives = 6;
int correctConnections = 0;
String currentCutString = "CBDA";
int currentCutNumber = 0;
bool showedCutAllWires = false;
bool _succeededLastCutMode = false;
bool _isInDebounceForCutMode = false;

void soundConnectMode(){
  _volume = _connectVolume;
  _freq = 800;
  _duration = 300;
  _offDuration = 700;
}

void soundCutMode() {
  _volume = _cutVolume;
  _freq = 1100;
  _duration = 200;
  _offDuration = 200;
}

void connectToWiFi() {
  Serial.print("Connecting to ");
  Serial.println(SSID);
  
  WiFi.begin(SSID, PWD);
  
  int tries = 0;
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(500);
    displayTwoWords((String("Bomb ") + String(tries)).c_str(), "initializing", true);
    if(tries++ > 100) {
      _soundOn = true;
      break;
    }
  }
 
  Serial.print("Connected. IP: ");
  Serial.println(WiFi.localIP());
}

void soundEndpoint() {
  Serial.println("audio endpoind reached");
  StaticJsonDocument<128> jsonDocument;
  String body = server.arg("plain");
  deserializeJson(jsonDocument, body);
  if(jsonDocument.containsKey("enable")) {
   _soundOn = bool(jsonDocument["enable"]);
  }
  if(jsonDocument.containsKey("volume")){
    _volume = int(jsonDocument["volume"]);
  }
  if(jsonDocument.containsKey("connectVolume")){
    _connectVolume = int(jsonDocument["connectVolume"]);
  }
  if(jsonDocument.containsKey("cutVolume")){
    _cutVolume = int(jsonDocument["cutVolume"]);
  }
  if(jsonDocument.containsKey("cutSuccessVolume")){
    _cutSuccessVolume = int(jsonDocument["cutSuccessVolume"]);
  }
  if(jsonDocument.containsKey("cutFailedVolume")){
    _cutFailedVolume = int(jsonDocument["cutFailedVolume"]);
  }
  if(jsonDocument.containsKey("enable")){
    _soundOn = bool(jsonDocument["enable"]);
  }  
  if(jsonDocument.containsKey("freq")){
    _freq = int(jsonDocument["freq"]);
  }
  if(jsonDocument.containsKey("duration")){
    _duration = int(jsonDocument["duration"]);
  }
  if(jsonDocument.containsKey("offDuration")){
    _offDuration = int(jsonDocument["offDuration"]);
  }
  server.send(204);
}

void gamestateEndpoint() {
  Serial.println("gamestate endpoind reached");
  StaticJsonDocument<128> jsonDocument;
  String body = server.arg("plain");
  deserializeJson(jsonDocument, body);
  if(jsonDocument.containsKey("bombCount")){
    remainingExplosives = int(jsonDocument["bombCount"]);
  }
  if(jsonDocument.containsKey("minutesRemaining")){
    totalTimeLimit = millis() + (int(jsonDocument["minutesRemaining"]) * 60000);
  }
 
  server.send(204);
}

void setup_routing() {      
  server.on("/sound", HTTP_POST, soundEndpoint);
  server.on("/gamestate", HTTP_POST, gamestateEndpoint);
  server.begin();
}

void initDisplay(){
  // Initialising the UI will init the display too.
  display.init();

  display.flipScreenVertically();
  display.setFont(ArialMT_Plain_10);
}

void setup() {
  Serial.begin(9600);
  
  // Set outputs for connections
  pinMode(pinE, OUTPUT);
  pinMode(pinF, OUTPUT);
  pinMode(pinG, OUTPUT);
  pinMode(pinH, OUTPUT);
  
  // Set inputs for connections
  pinMode(pinA, INPUT);
  pinMode(pinB, INPUT);
  pinMode(pinC, INPUT);
  pinMode(pinD, INPUT);

  // Initially set all outputs low
  digitalWrite(pinE, LOW);
  digitalWrite(pinF, LOW);
  digitalWrite(pinG, LOW);
  digitalWrite(pinH, LOW);
  
  initDisplay();
  
  connectToWiFi();
  setup_routing();  
}

void showedCutAllWiresTrue() {
  showedCutAllWires = true;
}

// Check connections and update flags
void updateCorrectConnections() {
  // Reset connection flags
  correctConnections = 0;
  isAEConnected = false;
  isBGConnected = false;
  isCHConnected = false;
  isDFConnected = false;

  // Set outputs to HIGH one at a time and check inputs

  digitalWrite(pinE, HIGH);
  delay(10); // Small delay to stabilize signal
  if (digitalRead(pinA)) {
    Serial.print("AE ");
    isAEConnected = true;
    correctConnections++;
  }
  digitalWrite(pinE, LOW);

  digitalWrite(pinG, HIGH);
  delay(10);
  if (digitalRead(pinB)) {
    Serial.print("BG ");
    isBGConnected = true;
    correctConnections++;
  }
  digitalWrite(pinG, LOW);

  digitalWrite(pinH, HIGH);
  delay(10);
  if (digitalRead(pinC)) {
    Serial.print("CH ");
    isCHConnected = true;
    correctConnections++;
  }
  digitalWrite(pinH, LOW);

  // Check GH connection
  digitalWrite(pinF, HIGH);
  delay(10);
  if (analogRead(pinD) > 1000) {
    Serial.print("DF");
    isDFConnected = true;
    correctConnections++;
  }
  digitalWrite(pinF, LOW);
}


//correct connections = AE BG CH DF
char checkForDisconnect() {
  updateCorrectConnections();
  char lastDiconnect = 'X';
  if (isAEConnected != wasAEConnected) {
    if (isAEConnected) {
      Serial.println("A connected");
    } else {
      Serial.println("A disconnected");
      lastDiconnect = 'A';
    }
    wasAEConnected = isAEConnected;
  }

  if (isBGConnected != wasBGConnected) {
    if (isBGConnected) {
      Serial.println("B connected");
    } else {
      Serial.println("B disconnected");
      lastDiconnect = 'B';
    }
    wasBGConnected = isBGConnected;
  }

  if (isCHConnected != wasCHConnected) {
    if (isCHConnected) {
      Serial.println("C connected");
    } else {
      Serial.println("C disconnected");
      lastDiconnect = 'C';
    }
    wasCHConnected = isCHConnected;
  }

  if (isDFConnected != wasDFConnected) {
    if (isDFConnected) {
      Serial.println("D connected");
    } else {
      Serial.println("D disconnected");
      lastDiconnect = 'D';
    }
    wasDFConnected = isDFConnected;
  }
  return lastDiconnect;
}

void DisplayTimeRemaining(const uint8_t* font, int y){
  int timeRemaining = totalTimeLimit - millis();
  String timeString = printTime(timeRemaining);
 
  display.setFont(font);
  display.setTextAlignment(TEXT_ALIGN_RIGHT);
  display.drawString(128, y, timeString);
}

String printTime(int millis)
{
  if(millis < 0){
    millis = 0;
  }
  char buffer[9];
// Assuming timeRemaining is in milliseconds
if (millis > 6000) {
    // Convert milliseconds to minutes and seconds
    int minutes = (millis / 60000); // Convert to minutes
    int seconds = (millis % 60000) / 1000; // Remaining milliseconds converted to seconds
    int centiseconds = (millis % 1000) / 10; // Remaining milliseconds converted to seconds
    sprintf(buffer, "%02d:%02d.%02d", minutes, seconds, centiseconds); // Use %02d to ensure two digits
} else {
    // Less than 6 seconds (6000 milliseconds)
    int seconds = (millis / 1000); // Convert to seconds
    int milliseconds = (millis % 1000) / 10; // Get tenths of a second
    sprintf(buffer, "%02d.%02d", seconds, milliseconds); // Use %02d for consistent formatting
}

  return String(buffer);
}


void EndCutMode(bool success){
  currentCutNumber = 0;
  showedCutAllWires = false;
  if(success) {
    remainingExplosives--;
  }
  _succeededLastCutMode = success;
  gotoCutMessageMode(success);
}

void gotoCutMessageMode(bool success) {
  currentMode = CUTMESSAGE;
  if(success) {
    _duration = 500;
    _offDuration = 500;
    _volume = _cutSuccessVolume;
    _freq = 800;
  }
  else {
    _duration = 50;
    _offDuration = 50;
    _volume = _cutFailedVolume;
    _freq = 3000;
  }
  timer.setTimeout(gotoConnectMode, _cutMessageDuration);
}

void DisplayText(const char* top, const char* bottom) {
  display.setTextAlignment(TEXT_ALIGN_CENTER);
  display.setFont(ArialMT_Plain_24);
  display.drawString(64, 0, top);
  display.setFont(ArialMT_Plain_24);
  display.drawString(64, 24, bottom);
}

void displayTwoWords(const char* top, const char* bottom, bool displayTime) {
  display.clear();
  DisplayText(top, bottom);
  if(displayTime){
    DisplayTimeRemaining(ArialMT_Plain_16, 48);
  }
  display.display();
}

bool checkCorrectCut(int currentCutNumber) {
    char disconnectedWire = checkForDisconnect();
    String alreadyCutWires = currentCutString.substring(0, currentCutNumber);
    Serial.println(alreadyCutWires);
    if(disconnectedWire != 'X') { 
      if(alreadyCutWires.indexOf(disconnectedWire) >= 0) {
        return false; //(second) disconnect of already correctly cut wire.
      }
      else if(disconnectedWire == currentCutString[currentCutNumber]) { //disconnect of correct wire.
        return true;        
      }
      else {
        EndCutMode(false);
      }
    }
    return false;
}

void checkCutMode() {
  if(currentCutNumber == 0) {
    displayTwoWords("CUT!!", "", true);
    if(checkCorrectCut(currentCutNumber)) {
      currentCutNumber++;
    }
  }
  if(currentCutNumber == 1) {
    if(!showedCutAllWires) {
      displayTwoWords("Cut all", "the wires", true);
      timer.setTimeout(showedCutAllWiresTrue, 30000);
    }
    else {
      displayTwoWords("Danger", "module", true);
      if(checkCorrectCut(currentCutNumber)) {
        currentCutNumber++;
      }
    }
  }
    if(currentCutNumber == 2) {
    displayTwoWords("Module", "module", true);
    if(checkCorrectCut(currentCutNumber)) {
      currentCutNumber++;
    }
  }
    if(currentCutNumber == 3) {
    displayTwoWords("Danger", "Do not touch", true);
    if(checkCorrectCut(currentCutNumber)) {
      currentCutNumber++;
    }
  }
  if(currentCutNumber == 4) {
    EndCutMode(true);
  }
}

void checkConnectMode() {
  updateCorrectConnections();
  if(correctConnections == 4 && !_isInDebounceForCutMode) {
    _isInDebounceForCutMode = true;
    timer.setTimeout(gotoCutMode, 2000);
  }
}

void gotoCutMode(){
    _isInDebounceForCutMode = false;
    currentMode = CUT;
    soundCutMode();
}

void gotoConnectMode() {
    if(remainingExplosives == 0){
      currentMode = DISARMED;
      _soundOn = false;
    }
    else {
      currentMode = CONNECT;
      soundConnectMode();
    }
}

void DisplayConnectStats() {
  display.setTextAlignment(TEXT_ALIGN_LEFT);
  display.setFont(ArialMT_Plain_24);
  display.drawString(0, 0, "  " + String(remainingExplosives));
  display.setFont(ArialMT_Plain_24);
  display.drawString(0, 32, "  " + String(4 - correctConnections));
}

void DisplayConnectMode() {
  display.clear();
  DisplayConnectStats();
  DisplayTimeRemaining(ArialMT_Plain_24, 20);
  // write the buffer to the display
  display.display();
}

void BeginBeep() {
  if(_soundOn){
    _beeping = true;
    pinMode(D1, OUTPUT);       // Set the pin to output mode
    analogWriteFreq(_freq);      // Set the PWM frequency for audio (2 kHz)
    analogWrite(D1, _volume);      // Generate a 50% duty cycle (produces a sound)
    timer.setTimeout(EndBeep, _duration);    
  }
  else {
    _startedBeep = false;
  }
    
}

void EndBeep() {
  analogWrite(D1, 0);        // Stop the PWM output (silence)
  pinMode(D1, INPUT);        // Switch the pin to input mode
  _beeping = false; 
  timer.setTimeout(BeginBeep, _offDuration);
}

void loop() {
  if(currentMode == CONNECT){
    if(!_beeping) {
      checkConnectMode();
    }    
    DisplayConnectMode(); 
  }
  
  else if(currentMode == CUT){
    if(!_beeping) {
      checkCutMode();
    }
  }

  else if(currentMode == CUTMESSAGE){
    if(_succeededLastCutMode){
      displayTwoWords("Disarmed", "Succesfully", true);
    }
    else {
      displayTwoWords("Disarming", "Failed", true);
    }
  }

  else if(currentMode == DISARMED){
      displayTwoWords("Explosive", "Disarmed!", false);
  }

  server.handleClient();
  timer.handle();  

  if(!_startedBeep){
    _startedBeep = true;
    BeginBeep();    
  }
  
  delay(10); // Loop delay for stability
}
