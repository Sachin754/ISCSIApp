// eslint-disable consistent-return */
import readline from 'readline';
import Promise from 'bluebird';
import { Request, TYPES } from 'tedious';
import ConnectionPool from 'tedious-connection-pool';
import ImageRight from 'imageright-api';
import config from './config';
import { getDrawer } from './src/drawer';
import logger from './utils/logger';
import { getLocalTimestamp } from './utils';
import { IRDocument } from './src/document';

const ApplicationName = 'imageright-import';

const FileTypeId = 63552563; // Stage and Test RT
// const FileTypeId = 90661562;
// const FileTypeId = 90661562;    // new files will have this type
// const FileTypeId = 86817437;    // Sandbox Archive type

const drawerConcurrency = 1;     // One Drawer at a time
const fileConcurrency = 1;       // Five Files per Drawer at a time
const folderConcurrency = 1;     // One Folder per File at a time
const documentConcurrency = 1;   // Ten Documents per Folder at a time
const errorThreshold = 10000;    // Stop processing after 100 errors;

// a positive integer, example 1000 to test first 1000 documents.
const limit = 0; // 0 means no limit

let filesPerPage = 100000; // # of files to pull back at a time from database

// set filesPerPage to limit if limit is smaller
filesPerPage = (limit < filesPerPage && limit > 0) ? limit : filesPerPage;

let page = 0; // will track paging of data, 0 is first page
let fileCount; // will track count of files pulled from DB
let completedcount = 0; // will track completed files that didn't error

// Check if there is a maxDocs specified
const maxDocs = limit === 0 ? false : limit;

const selectSQL = `
SELECT
    [ID],
    [IRDrawer],
    [IRFileID],
    [IRFileNumber],
    [IRFileName],
    [SRC_Att_New/Renew] as SRC_Att_New_Renew,
    [SRC_Att_Market_Name] as SRC_Att_Market_Name,
    [SRC_Att_Effective_Date],
    [SRC_Att_Expiration_Date],
    [SRC_Att_Coverage],
    [SRC_Att_Policy_Number],
    [SRC_Att_Policy_Status],
    [SRC_Att_Assistant],
    [SRC_Att_Broker],
    [StatusId]
FROM [MAP].[ImageRightFiles_Attributes]
WHERE StatusId = 0
AND ID BETWEEN 5584 AND 5585
ORDER BY ID
OFFSET ${page} ROWS FETCH NEXT ${filesPerPage} ROWS ONLY`;

const updateSQL = `
UPDATE [MAP].[ImageRightFiles_Attributes]
SET
    IRFileID = @IRFileID,
    StatusID = @StatusID,
    StatusMessage = @StatusMessage,
    StatusDate = @StatusDate
WHERE ID = @ID`;

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'; // allow self-signed SSL certificates

if (process.platform === 'win32') {
    readline.createInterface({
        input: process.stdin,
        output: process.stdout,
    })
        .on('SIGINT', () => {
            process.emit('SIGINT');
        });
}

let errorCount = 0;
let sigintReceived = false;

process.on('SIGINT', () => {
    // graceful shutdown
    sigintReceived = true;
});

const pool = new ConnectionPool({ min: 1, max: 50, log: false }, config.tedious);

pool.on('error', (err) => {
    logger.error(err);
});

const irlib = new ImageRight(config.imageright.baseUrl)
    .connect(config.imageright.username, config.imageright.password);

const updateMapFiles = (status, doc, msg) => new Promise((resolve, reject) => {
    pool.acquire((err, connection) => {
        if (err) {
            return reject(err);
        }
        const request = new Request(updateSQL, (insertErr, rowCount) => {
            connection.release();
            if (insertErr) {
                return reject(insertErr);
            }
            resolve(rowCount);
        });

        if (status === -1) { // console.log("status"); console.log('err'); process.exit();
            completedcount += 1;
        }

        // console.log(JSON.stringify(doc)); process.exit();
        // console.log('--+doc.IRFileID', doc.IRFileID); console.log('--+status', status); console.log('--+JSON.stringify(msg)', JSON.stringify(msg));
        // console.log('--+doc.ID', doc.ID); console.log('--+msg.documentId', msg.documentId); process.exit();
        request.addParameter('IRFileID', TYPES.Int, msg.id);
        request.addParameter('StatusID', TYPES.Int, status);
        request.addParameter('StatusMessage', TYPES.VarChar, JSON.stringify(doc));
        request.addParameter('StatusDate', TYPES.DateTime, new Date());
        request.addParameter('ID', TYPES.Int, doc.ID);
        // console.error(request); process.exit();
        connection.execSql(request);
    });
});

class FailEarlyError extends Error { }

const errorCheck = () => {
    if (sigintReceived) throw new FailEarlyError('SIGINT received');
    if (fileCount === 0) throw new FailEarlyError(`No files: Files: ${fileCount} Error Count (${errorCount})`);
    if (errorCount >= fileCount) throw new FailEarlyError(`Error Count (${errorCount}) Files: ${fileCount}`);
    if (errorCount >= errorThreshold) throw new FailEarlyError(`Error Threshold (${errorThreshold}) Reached: ${errorCount}`);
};

const errorHandler = (doc) => (err) => {
    console.log("doc---", doc);
    console.log("error---testing---", err); //process.exit();
    errorCount += 1;
    logger.info(`Error Count: ${errorCount}`);
    if (err instanceof FailEarlyError) return logger.error(err);
    if (err.stack) logger.error(err.stack);
    if (err.response) {
        const { method, url, data } = err.response.config;
        const { status, statusText, data: body } = err.response;
        const msg = {
            request: { method, url, data },
            response: { status, statusText, body },
        };
        logger.error(msg);
        return updateMapFiles(-1, doc, msg);
    } else {
        return updateMapFiles(-2, doc, err.message);
    }
};

const appendFileId = (doc, folder) => {
    //console.log(doc); process.exit();
    const copy = { ...doc };
    copy.IRFileID = doc.IRPageID;
    return copy;
};

const processUpdate = promise => ([IRFolderName, docs]) => {
    logger.info('Starting Update:');
    // console.log(docs); process.exit();
    // let IRFileID; docs.forEach(item => { IRFileID = item.IRFileID; });
    return promise.then(msg => updateMapFiles(1, appendFileId(docs[0], IRFolderName), msg)
        .catch(errorHandler(docs[0])));
};

const processFile = promise => ([IRFileNumber, { IRFileID, IRFileName, IREffective, IRExpiration, IRCoverage, IRPolicyNumber, IRPolicyStatus, IRRenew, IRAssistant, IRBroker, IRMarket, folders }]) => {
    logger.info(`Starting File: ${IRFileNumber} ${IRFileName}`);
    const file = promise.then(drawer => drawer.getFileByNumber(IRFileNumber)
        .then(data => {
            const TargetFileID = data.id;
            logger.info(`Updating Properties: ${IRFileNumber}`);
            drawer.updateFileProperties(TargetFileID, FileTypeId, IRFileNumber,
                IREffective, IRExpiration, IRCoverage, IRPolicyNumber, IRPolicyStatus, IRRenew, IRAssistant, IRBroker, IRMarket);
            return data;
        })
        .catch(() => {
            return drawer.createFile(
                FileTypeId,
                IRFileNumber,
                IRFileName,
                IREffective,
                IRExpiration,
                IRCoverage,
                IRPolicyNumber,
                IRPolicyStatus,
                IRRenew,
                IRAssistant,
                IRBroker,
                IRMarket
            );
        }));

    return Promise.map(folders.entries(), processUpdate(file), { concurrency: folderConcurrency });

    // const file = promise.then(drawer => drawer.getFileByNumber(IRFileNumber))
};

const processDrawer = ([IRDrawerName, files]) => {
    logger.info(`Starting Drawer: ${IRDrawerName}`);
    const drawer = getDrawer(irlib, IRDrawerName, ApplicationName);
    return Promise.map(files.entries(), processFile(drawer), { concurrency: fileConcurrency });
};

const getData = () => new Promise((resolve, reject) => {
    pool.acquire((err, connection) => {
        if (err) return reject(err);
        const drawers = new Map();
        let count = 0;

        const request = new Request(selectSQL, (reqErr) => {
            connection.release();
            return (reqErr)
                ? reject(reqErr)
                : resolve({ drawers, count });
        });

        request.on('row', (columns) => {
            count += 1;
            const item = { count };
            columns.forEach((column) => {
                item[column.metadata.colName] = column.value;
            });

            if (!drawers.has(item.IRDrawer)) {
                drawers.set(item.IRDrawer, new Map());
            }
            const drawer = drawers.get(item.IRDrawer);
            if (!drawer.has(item.IRFileNumber)) {
                const addmins = 19000 * 1000;
                const IREffective_temp = new Date(item.SRC_Att_Effective_Date);
                const IRExpiration_temp = new Date(item.SRC_Att_Expiration_Date);
                const IREffective_conv = new Date(IREffective_temp.getTime() + addmins).toISOString();
                const IRExpiration_conv = new Date(IRExpiration_temp.getTime() + addmins).toISOString();

                const IRFileID = item.IRFileID;
                const IRFileName = item.IRFileName;
                const IREffective = IREffective_conv;
                const IRExpiration = IRExpiration_conv;
                const IRCoverage = item.SRC_Att_Coverage;
                const IRPolicyNumber = item.SRC_Att_Policy_Number;
                const IRPolicyStatus = item.SRC_Att_Policy_Status;
                const IRRenew = item.SRC_Att_New_Renew;
                const IRAssistant = item.SRC_Att_Assistant;
                const IRBroker = item.SRC_Att_Broker;
                const IRMarket = item.SRC_Att_Market_Name;

                drawer.set(item.IRFileNumber, {
                    IRFileID,
                    IRFileName,
                    IREffective,
                    IRExpiration,
                    IRCoverage,
                    IRPolicyNumber,
                    IRPolicyStatus,
                    IRRenew,
                    IRAssistant,
                    IRBroker,
                    IRMarket,
                    folders: new Map()
                });
            }
            const file = drawer.get(item.IRFileNumber);
            if (!file.folders.has(item.IRDocFolder)) {
                file.folders.set(item.IRDocFolder, []);
            }
            const folder = file.folders.get(item.IRDocFolder);
            folder.push(item);
        });

        connection.execSql(request);
    });
});

const cleanExit = (exitReason, stats) => {
    logger.info(`${getLocalTimestamp()} clean exit... saving logs...`, stats);
    if (exitReason) {
        logger.info(exitReason, stats);
    }
    // allow sometime for logs to finish writing
    process.nextTick(() => {
        setTimeout(process.exit, 1000);
    });
};

async function execMain(fmain) {
    //console.log("call exec main function"); process.exit();
    try {
        await fmain();
    } catch (error) {
        logger.info('main error catch.');
        logger.error(error);
        if (sigintReceived) cleanExit('sigInt received');
    }
}

const main = async () => {
    console.log("Starting Program");
    logger.info(`${getLocalTimestamp()} Starting...`);
    try {
        const promiseData = await Promise.all([getData()]);
        const { drawers, count } = promiseData[0];
        fileCount = count;
        errorCheck();
        logger.info(`Total Records: ${count}`, { startingCount: count });
        await Promise.map(drawers.entries(), processDrawer, { concurrency: drawerConcurrency });
    } catch (error) {
        logger.error('Error in catch %0', error);
        process.exit();
    } finally {
        // Exit if maxDocs is reached or fileCount is 0
        // Give an update if fileCount is not 0 and proceed to fetch next page of documents
        // From DB and run main function again, otherwise exit process when there are no more docs
        const totalDocs = errorCount + completedcount;
        if ((maxDocs && totalDocs >= maxDocs) || fileCount === 0) {
            pool.drain();
            const stats = { maxDocs, page, fileCount, completedcount, errorCount, totalDocs };
            cleanExit(`Finished: ${JSON.stringify(stats)}`, stats);
        } else if (fileCount > 0) {
            logger.info(`${getLocalTimestamp()} ${{ fileCount, page, completedcount, errorCount}}`);
            page +=filesPerPage;
            execMain(main);
        } else {
            const stats = { maxDocs, page, fileCount, completedcount, errorCount, totalDocs };
            cleanExit(`Strange finish: ${JSON.stringify(stats)}`, stats);
        }
    }
};

process.on("beforeExit", async () => {
    logger.info("before exit");
    await setTimeout(() =>{
        logger.info("before exit logged");
    }, 2000);
});

process.on('uncaughtException', (e) => {
    logger.error('uncaught exception %O', e);
    logger.error('%O', e.stack);
    cleanExit('uncaughtException');
});

execMain(main);
logger.info(`${getLocalTimestamp()} index.js running`);
