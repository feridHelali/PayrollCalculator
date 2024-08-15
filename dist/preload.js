/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./src/main/preload.ts":
/*!*****************************!*\
  !*** ./src/main/preload.ts ***!
  \*****************************/
/***/ ((__unused_webpack_module, exports, __webpack_require__) => {

eval("\nObject.defineProperty(exports, \"__esModule\", ({ value: true }));\nconst { contextBridge, ipcRenderer } = __webpack_require__(/*! electron */ \"electron\");\ncontextBridge.exposeInMainWorld('electronAPI', {\n    fetchAgreements: () => ipcRenderer.invoke('fetch-agreements'),\n    fetchAgreementById: (id) => ipcRenderer.invoke('fetch-agreement-by-id', id),\n    createAgreement: (agreement) => ipcRenderer.invoke('create-agreement', agreement),\n    updateAgreement: (agreement) => ipcRenderer.invoke('update-agreement', agreement),\n    deleteAgreement: (id) => ipcRenderer.invoke('delete-agreement', id),\n    fetchAllSalaryTables: () => ipcRenderer.invoke('fetch-all-salary-tables'),\n    fetchSalaryTableById: (id) => ipcRenderer.invoke('fetch-salary-table-by-id', id),\n    fetchSalaryTablesByAgreementId: (agreementId) => ipcRenderer.invoke('fetch-salary-tables-by-agreement-id', agreementId),\n    createSalaryTable: (salaryTable) => ipcRenderer.invoke('create-salary-table', salaryTable),\n    updateSalaryTable: (salaryTable) => ipcRenderer.invoke('update-salary-table', salaryTable),\n    deleteSalaryTable: (id) => ipcRenderer.invoke('delete-salary-table', id),\n    fetchAffairs: () => ipcRenderer.invoke('fetch-affairs'),\n    fetchAffairById: (id) => ipcRenderer.invoke('fetch-affair-by-id', id),\n    createAffair: (affair) => ipcRenderer.invoke('create-affair', affair),\n    updateAffair: (affair) => ipcRenderer.invoke('update-affair', affair),\n    deleteAffair: (id) => ipcRenderer.invoke('delete-affair', id),\n});\n\n\n//# sourceURL=webpack://payrollcalculator/./src/main/preload.ts?");

/***/ }),

/***/ "electron":
/*!***************************!*\
  !*** external "electron" ***!
  \***************************/
/***/ ((module) => {

module.exports = require("electron");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	var __webpack_exports__ = __webpack_require__("./src/main/preload.ts");
/******/ 	
/******/ })()
;