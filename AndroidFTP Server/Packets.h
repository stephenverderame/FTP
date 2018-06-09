#pragma once
#pragma comment(lib, "Ws2_32.lib")
#include <Windows.h>
#include <memory>
#include <string>
#define OK 0
#define MAX_CON_REACH -20
#define SOCK_CORRUPTION -30
enum Packets {
	p_fileSend,
	p_fileGet,
	p_dirSend,
	p_disconnect,
	p_formatPath,
	p_screenshot,
	p_execute,
	p_isFile = -1
};
int sendData(char * data, uint32_t size, SOCKET sock) {
	uint32_t amountSent = 0;
	while (amountSent < size) {
		uint32_t res = send(sock, data + amountSent, size - amountSent, NULL);
		if (res == SOCKET_ERROR) return SOCKET_ERROR;
		amountSent += res;
	}
	return OK;
}
int sendInt(int32_t data, SOCKET sock) {
	char amountSent = 0;
	data = htonl(data);
	while (amountSent < sizeof(int32_t)) {
		char res = send(sock, ((char*)&data) + amountSent, sizeof(int32_t), NULL);
		if (res == SOCKET_ERROR) return SOCKET_ERROR;
		amountSent += res;
	}
	return OK;
}
int getInt(int32_t & data, SOCKET sock) {
	char amountSent = 0;
	int32_t buffer;
	while (amountSent < sizeof(int32_t)) {
		char res = recv(sock, ((char*)&data) + amountSent, sizeof(int32_t), NULL);
		if (res == SOCKET_ERROR) return SOCKET_ERROR;
		amountSent += res;
	}
//	data = ntohl(buffer);
	return OK;
}
int getString(std::string & data, SOCKET sock, uint32_t size) {
	uint32_t amountSent = 0;
	char * buffer = new char[size + 1];
	while (amountSent < size) {
		uint32_t res = recv(sock, buffer + amountSent, size - amountSent, NULL);
		if (res == SOCKET_ERROR) return SOCKET_ERROR;
		amountSent += res;
	}
	buffer[size] = '\0';
	data = buffer;
	delete[] buffer;
	return OK;
}
std::string checkPath(std::string path) {
	if (path.find("XF!") != path.npos) {
		std::string nPath = path.substr(path.find('!') + 1, path.find('-') - 3);
		std::string rPath = path.size() - 1 > path.find('-') ? path.substr(path.find('-') + 1) : "";
		printf("Path size: %d : Location of - : %d \n", path.size(), path.find('-'));
		printf("Char at 1 minus - : %c \n", path[path.find('-') - 1]);
		printf("After path: %s \n", rPath.c_str());
		char fPath[MAX_PATH];
		sprintf_s(fPath, "%s\\%s%s", getenv("USERPROFILE"), nPath.c_str(), rPath.c_str());
		return std::string(fPath);
	}
	return path;
}