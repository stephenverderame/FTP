#include <cstdio>
#include <vector>
#include <thread>
#include <mutex>
#include "Files.h"
#pragma comment(lib, "Gdiplus.lib")
#include <gdiplus.h>
#define _WINSOCK_DEPRECATED_NO_WARNINGS
#define MAX_CONNECTIONS 6
char currentConnectedNum = 0;
std::mutex consoleMu;
int getEncoderClsid(const WCHAR * format, CLSID * pClsid) {
	UINT num = 0; //num of image encoders
	UINT size = 0; //size of image encoder array
	Gdiplus::ImageCodecInfo * imageCodec = NULL;
	Gdiplus::GetImageEncodersSize(&num, &size);
	if (size == 0) {
		return -1;
	}
	imageCodec = (Gdiplus::ImageCodecInfo*)(malloc(size));
	if (imageCodec == NULL) {
		return -1;
	}
	Gdiplus::GetImageEncoders(num, size, imageCodec);
	for (UINT i = 0; i < num; ++i) {
		if (wcscmp(imageCodec[i].MimeType, format) == 0) {
			//strcmp for wide chars - performs camparison of two strings
			// <0 string1 < string 2
			// 0 both strings are identical
			// >0 string 1 > string 2
			*pClsid = imageCodec[i].Clsid;
			free(imageCodec);
			return i;
		}
	}
	free(imageCodec);
	return -1;

}
void handleClient(SOCKET sock, char * client) {
	int errorCode;
	bool connection = true;
	do {
		int32_t packet;
		if ((errorCode = getInt(packet, sock)) != OK) break;
		switch (packet) {
		case p_disconnect:
		{
			std::lock_guard<std::mutex> guard(consoleMu);
			printf("Got disconnect packet! \n");
			errorCode = OK;
			connection = false;
			break;
		}
		case p_dirSend:
		{
			{
				std::lock_guard<std::mutex> guard(consoleMu);
				printf("Sending directory! \n");
			}
			int32_t pathSize;
			if ((errorCode = getInt(pathSize, sock)) != OK) break;
			std::string path;
			if ((errorCode = getString(path, sock, pathSize)) != OK) break;
			{
				std::lock_guard<std::mutex> guard(consoleMu);
				printf("Got path %s with size of %d! \n", path.c_str(), pathSize);
			}
			std::string fPath = checkPath(path);
			{
				std::lock_guard<std::mutex> guard(consoleMu);
				printf("New path %s with size of %d! \n", fPath.c_str(), fPath.size());
			}
			if (!isFile(fPath)) {
				std::vector<std::string> files = getDirectory(fPath);
				int amountOfFiles = files.size();
				if ((errorCode = sendInt(amountOfFiles, sock)) != OK) break;
				for (int i = 0; i < amountOfFiles; i++) {
					std::lock_guard<std::mutex> guard(consoleMu);
					int nameSize = files[i].size();
					std::string name = files[i];
					if ((errorCode = sendInt(nameSize, sock)) != OK) break;
//					printf("Sending name: %s \n", name.c_str());
					char * namePtr = new char[nameSize + 1];
					strcpy_s(namePtr, nameSize + 1, name.c_str());
					namePtr[nameSize] = '\0';
					if ((errorCode = sendData(namePtr, nameSize, sock)) != OK) break;					
//					printf("Sent path %s with size of %d! \n", name.c_str(), nameSize);
					delete[] namePtr;
				}
				if (errorCode != OK) break;
			}
			else {
				int packet = p_isFile;
				if ((errorCode = sendInt(packet, sock)) != OK) break;
				{
				std::lock_guard<std::mutex> guard(consoleMu);
				{
					printf("Attemped to send directory of a file, moving to file send\n");
				}
				}
				int32_t fileSize;
				getFileSize(fPath, fileSize);
//				printf("File size of %s: %d \n", fPath.c_str(), fileSize);
				int flag = fPath.find(".txt") != fPath.npos ? TEXT : BINARY;
				char * file = new char[fileSize];
//				loadFile(fPath, &file, fileSize, flag);
				if (flag == BINARY) {
					std::ifstream reader;
					reader.open(fPath, std::ios::binary);
					reader.rdbuf()->sgetn(file, fileSize);
					reader.close();
//					writeFile("Test.wav", file, fileSize);

				}
				else {
					loadFile(fPath, &file, fileSize, TEXT);
				}
//				printf("File data: %s \n", file);
//				if (flag == TEXT) printf("File is text!");
				if ((errorCode = sendInt(fileSize, sock)) != OK) break;
//				printf("Sent file size of: %d bytes \n", fileSize);
				if ((errorCode = sendData(file, fileSize, sock)) != OK) break;
//				printf("Sent file data");
				delete[] file;
				

			}
			break;
		}
		case p_fileGet:
		{
			int nameSize;
			if ((errorCode = getInt(nameSize, sock)) != OK) break;
			std::string name;
			if ((errorCode = getString(name, sock, nameSize)) != OK) break;
			int fileSize;
			if ((errorCode = getInt(fileSize, sock)) != OK) break;
			if ((errorCode = getFile(fileSize, name, sock)) != OK) break;
			std::lock_guard<std::mutex> guard(consoleMu);
			printf("Recievd File! \n");
			break;

		}
		case p_screenshot:
		{
			HDC screenDc = GetDC(NULL);
			HDC memDc = CreateCompatibleDC(screenDc);
			int width = GetDeviceCaps(screenDc, HORZRES);
			int height = GetDeviceCaps(screenDc, VERTRES);
			HBITMAP image = CreateCompatibleBitmap(screenDc, width, height);
			HBITMAP oldBitmap = (HBITMAP)SelectObject(memDc, image);
			BitBlt(memDc, 0, 0, width, height, screenDc, 0, 0, SRCCOPY);
			DeleteDC(screenDc);
			DeleteDC(memDc);


			Gdiplus::GdiplusStartupInput gdiplusStart;
			ULONG_PTR gdiplusToken;
			Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStart, NULL);
			Gdiplus::Bitmap * bit = new Gdiplus::Bitmap(image, NULL);
			CLSID Clsid;
			int ret = getEncoderClsid(L"image/bmp", &Clsid);
			bit->Save(L"screenshot.bmp", &Clsid, NULL);
			delete bit;
			Gdiplus::GdiplusShutdown(gdiplusToken);

			int32_t fileSize;
			std::ifstream stream;
			stream.open("screenshot.bmp", std::ios::binary | std::ios::ate);
			fileSize = stream.tellg();
			char * buffer = new char[fileSize];
			stream.seekg(0, std::ios::beg);
			stream.rdbuf()->sgetn(buffer, fileSize);
			stream.close();
			std::lock_guard<std::mutex> guard(consoleMu);
			printf("Sending bitmap of size: %d \n", fileSize);
			if ((errorCode = sendInt(fileSize, sock)) != OK) break;
			if ((errorCode = sendData(buffer, fileSize, sock)) != OK) break;
			delete[] buffer;
			break;
		}
		case p_execute:
		{
			int32_t pathSize;
			if ((errorCode = getInt(pathSize, sock)) != OK) break;
			std::string path;
			if ((errorCode = getString(path, sock, pathSize)) != OK) break;
			std::string fPath = checkPath(path);
			if (!isFile(fPath)) {
				std::vector<std::string> files = getDirectory(fPath);
				int amountOfFiles = files.size();
				if ((errorCode = sendInt(amountOfFiles, sock)) != OK) break;
				for (int i = 0; i < amountOfFiles; i++) {
					std::lock_guard<std::mutex> guard(consoleMu);
					int nameSize = files[i].size();
					std::string name = files[i];
					if ((errorCode = sendInt(nameSize, sock)) != OK) break;
					char * namePtr = new char[nameSize + 1];
					strcpy_s(namePtr, nameSize + 1, name.c_str());
					namePtr[nameSize] = '\0';
					if ((errorCode = sendData(namePtr, nameSize, sock)) != OK) break;
					delete[] namePtr;
				}
				if (errorCode != OK) break;
			}
			else {
				int packet = p_isFile;
				if ((errorCode = sendInt(packet, sock)) != OK) break;
				CoInitialize(NULL);
				if (fPath.find(".exe") == fPath.npos) {
					ShellExecute(NULL, NULL, fPath.c_str(), NULL, NULL, SW_SHOW);
				}
				else {
					STARTUPINFO info;
					PROCESS_INFORMATION pinfo;
					SECURITY_ATTRIBUTES security;
					ZeroMemory(&info, sizeof(info));
					ZeroMemory(&pinfo, sizeof(pinfo));
					ZeroMemory(&security, sizeof(security));
					security.bInheritHandle = TRUE;
					security.nLength = sizeof(SECURITY_ATTRIBUTES);
					security.lpSecurityDescriptor = NULL;
					info.cb = sizeof(STARTUPINFO);
					CreateProcess(fPath.c_str(), NULL, &security, &security, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &info, &pinfo);

				}
				CoUninitialize();
			}
			break;
		}
		default:
		{
			std::lock_guard<std::mutex> guard(consoleMu);
			printf("Unknown packet: %d \n", packet);
			printf("Socket corruption assumed, disconnecting client... \n");
			errorCode = SOCK_CORRUPTION;
			connection = false;
			break;
		}
		}
		if (errorCode != OK) {
			connection = false;
			break;
		}
	} while (connection);
	std::lock_guard<std::mutex> guard(consoleMu);
	if (errorCode == OK) {
		printf("Client %s disconnected! \n", client);
	}
	else {
		printf("Client %s connection closed: %d \n", client, errorCode);
	}
	closesocket(sock);
	currentConnectedNum--;
}
int main() {
	WSAData data;
	SOCKADDR_IN address;
	if (WSAStartup(MAKEWORD(2, 1), &data) != 0) {
		std::lock_guard<std::mutex> guard(consoleMu);
		printf("Startup failed %d \n", WSAGetLastError());
		system("pause");
		return 1;
	}
	SOCKET sck_connection = socket(AF_INET, SOCK_STREAM, NULL);
	address.sin_addr.s_addr = INADDR_ANY;
	address.sin_port = htons(3250);
	address.sin_family = AF_INET;
	SOCKET sck_listen = socket(AF_INET, SOCK_STREAM, NULL);
	bind(sck_listen, (SOCKADDR*)&address, sizeof(address));
	listen(sck_listen, SOMAXCONN);
	int addrSize = sizeof(address);
	{
		std::lock_guard<std::mutex> guard(consoleMu);
		printf("Server started! \n");
/*		int size;
		std::ifstream reader;
		reader.open("185.JPG", std::ios::binary | std::ios::ate);
		size = reader.tellg();
		printf("File size: %d \n", size);
		char * buffer = new char[size];
		reader.seekg(0, std::ios::beg);
		reader.rdbuf()->sgetn(buffer, size);
		reader.close();
		std::ofstream writer;
		writer.open("186.JPG", std::ios::binary);
		writer.write(buffer, size);
		writer.close();
/*		getFileSize("185.JPG", size);
		char * data = new char[size];
		loadFile("185.JPG", &data, size, BINARY);
		writeFile("185Write.JPG", data, size);
		delete[] data;
/*		std::vector<std::string> files = getDirectory("c:");
		for (std::string s : files) {
			printf(":: %s \n", s.c_str());
		}
		*/
	}
	while (true) {
		if (sck_connection = accept(sck_listen, (SOCKADDR*)&address, &addrSize)) {
			SOCKADDR_IN connected;
			ZeroMemory(&connected, sizeof(connected));
			getpeername(sck_connection, (SOCKADDR*)&connected, &addrSize);
			char * ip = inet_ntoa(connected.sin_addr);
			if (currentConnectedNum < MAX_CONNECTIONS) {
				currentConnectedNum++;
				{
					std::lock_guard<std::mutex> guard(consoleMu);
					printf("Connection established with: %s \n", ip);
					printf("Ip strlen is %d \n", strlen(ip));
				}
				std::thread handler(handleClient, sck_connection, ip);
				sendInt(strlen(ip), sck_connection);
				sendData(ip, strlen(ip), sck_connection);
				handler.detach();
			}
			else
			{
				sendInt(MAX_CON_REACH, sck_connection);
			}
		}
	}
	system("pause");
	return 0;
}