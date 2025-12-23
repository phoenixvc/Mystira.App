// Loading Progress Tracker - Manages staged loading experience with real progress indicators

const LOG_PREFIX = '[MystiraLoading]';

// Initialize loading tracker when DOM is ready, but don't auto-start
let loadingInitialized = false;

window.mystiraLoading = (function() {
    // Loading stages configuration
    const LOADING_STAGES = [
        {
            id: 'initializing',
            message: 'Starting Mystira…',
            icon: 'fas fa-rocket',
            optional: false
        },
        {
            id: 'checking-connection',
            message: 'Checking connection…',
            icon: 'fas fa-wifi',
            optional: false
        },
        {
            id: 'loading-library',
            message: 'Loading your library…',
            icon: 'fas fa-book-open',
            optional: false
        },
        {
            id: 'preparing-engine',
            message: 'Preparing story engine…',
            icon: 'fas fa-cogs',
            optional: false
        },
        {
            id: 'service-worker-update',
            message: 'Updating Mystira… (new version)',
            icon: 'fas fa-download',
            optional: true,
            conditional: () => window.swUpdateAvailable
        },
        {
            id: 'downloading-content',
            message: 'Downloading new content…',
            icon: 'fas fa-cloud-download-alt',
            optional: true,
            conditional: () => window.contentNeedsUpdate
        },
        {
            id: 'almost-ready',
            message: 'Almost ready…',
            icon: 'fas fa-sparkles',
            optional: false
        }
    ];

    let currentStageIndex = 0;
    let startTime = Date.now();
    let timeoutTimer = null;
    let isComplete = false;
    let progressData = {
        downloadProgress: null,      // { current: number, total: number }
        cacheProgress: null,         // { current: number, total: number }
        syncProgress: null           // { current: number, total: number }
    };
    
    let eventHandlers = {
        onStageChange: null,
        onProgressUpdate: null,
        onTimeoutWarning: null,
        onTimeout: null,
        onComplete: null
    };

    // Initialize loading tracker
    function initialize() {
        console.log(LOG_PREFIX, 'Initializing loading tracker');
        startTime = Date.now();
        currentStageIndex = 0;
        isComplete = false;
        
        // Start timeout timer (15 seconds)
        startTimeoutTimer();
        
        // Add timeout warning timer (10 seconds)
        startTimeoutWarningTimer();
        
        // Listen for service worker messages
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.addEventListener('message', handleServiceWorkerMessage);
        }
        
        // Set up blazor update notifications
        setupBlazorIntegration();
        
        // Check connectivity status
        checkConnectivity();
        
        // Start with first stage
        updateStage('initializing');
    }

    // Start timeout warning timer (10 seconds)
    function startTimeoutWarningTimer() {
        setTimeout(() => {
            if (!isComplete) {
                console.log(LOG_PREFIX, 'Showing timeout warning (10s)');
                if (eventHandlers.onTimeoutWarning) {
                    eventHandlers.onTimeoutWarning();
                } else {
                    console.warn(LOG_PREFIX, 'Timeout warning handler not set');
                }
            }
        }, 10000);
    }

    // Check connectivity status
    function checkConnectivity() {
        if ('connection' in navigator) {
            const connection = navigator.connection;
            const connectionType = connection.effectiveType;
            console.log(LOG_PREFIX, 'Connection type:', connectionType);
            
            // If slow connection, adjust expectations
            if (connectionType === 'slow-2g' || connectionType === '2g') {
                console.log(LOG_PREFIX, 'Slow connection detected, extending timeouts');
                // Extend timeout warning to 15 seconds and final timeout to 25 seconds
                clearTimeout(timeoutTimer);
                timeoutTimer = setTimeout(() => {
                    console.warn(LOG_PREFIX, 'Loading timeout reached (25s for slow connection)');
                    if (!isComplete && eventHandlers.onTimeout) {
                        eventHandlers.onTimeout();
                    }
                }, 25000);
            }
            
            // Listen for connection changes
            connection.addEventListener('change', () => {
                console.log(LOG_PREFIX, 'Connection changed to:', connection.effectiveType);
            });
        }
    }

    // Handle messages from service worker
    function handleServiceWorkerMessage(event) {
        console.log(LOG_PREFIX, 'Service worker message:', event.data);
        
        switch (event.data?.type) {
            case 'CACHE_PROGRESS':
                updateProgress('cache', event.data.current, event.data.total);
                break;
            case 'DOWNLOAD_PROGRESS':
                updateProgress('download', event.data.current, event.data.total);
                break;
            case 'SYNC_PROGRESS':
                updateProgress('sync', event.data.current, event.data.total);
                break;
            case 'SERVICE_WORKER_UPDATE':
                window.swUpdateAvailable = true;
                console.log(LOG_PREFIX, 'Service worker update available');
                break;
        }
    }

    // Set up Blazor JSInterop integration
    function setupBlazorIntegration() {
        // Register with Blazor for content update notifications
        if (window.mystiraContentUpdater) {
            window.mystiraContentUpdater.setProgressCallback(updateProgress);
        }
    }

    // Update current loading stage
    function updateStage(stageId) {
        console.log(LOG_PREFIX, 'Updating stage to:', stageId);
        
        const stageIndex = LOADING_STAGES.findIndex(stage => stage.id === stageId);
        if (stageIndex === -1) {
            console.warn(LOG_PREFIX, 'Unknown stage:', stageId);
            return;
        }

        currentStageIndex = stageIndex;
        
        if (eventHandlers.onStageChange) {
            const visibleStages = getVisibleStages();
            const stageData = {
                currentStage: LOADING_STAGES[stageIndex],
                completedStages: visibleStages.slice(0, stageIndex + 1),
                allStages: visibleStages
            };
            
            // Call the event handler with the data object
            eventHandlers.onStageChange(stageData);
        }
        
        // Auto-progress to next stage after a delay (unless it's the last stage)
        if (stageIndex < LOADING_STAGES.length - 1) {
            setTimeout(() => {
                const nextStageId = LOADING_STAGES[stageIndex + 1].id;
                console.log(LOG_PREFIX, 'Auto-progressing to next stage:', nextStageId);
                updateStage(nextStageId);
            }, 2000); // 2 seconds per stage
        } else {
            // Last stage reached, complete after a moment
            setTimeout(() => {
                console.log(LOG_PREFIX, 'All stages complete, finishing...');
                complete();
            }, 1500);
        }
    }

    // Update progress for specific operation
    function updateProgress(operation, current, total) {
        if (!progressData[operation + 'Progress']) {
            progressData[operation + 'Progress'] = { current: 0, total: 0 };
        }
        
        progressData[operation + 'Progress'].current = current;
        progressData[operation + 'Progress'].total = total;
        
        if (eventHandlers.onProgressUpdate) {
            const progressUpdate = {
                operation: operation,
                current: current,
                total: total
            };
            
            // Call the event handler with the progress data
            eventHandlers.onProgressUpdate(progressUpdate);
        }
    }

    // Mark loading as complete
    function complete() {
        if (isComplete) return;
        
        console.log(LOG_PREFIX, 'Loading complete');
        isComplete = true;
        clearTimeout(timeoutTimer);
        
        if (eventHandlers.onComplete) {
            eventHandlers.onComplete();
        }
    }

    // Start timeout timer (15 seconds)
    function startTimeoutTimer() {
        timeoutTimer = setTimeout(() => {
            console.warn(LOG_PREFIX, 'Loading timeout reached (15s)');
            if (!isComplete) {
                if (eventHandlers.onTimeout) {
                    eventHandlers.onTimeout();
                } else {
                    console.warn(LOG_PREFIX, 'Timeout handler not set');
                }
            }
        }, 15000);
    }

    // Get visible stages (filtering out optional ones that don't apply)
    function getVisibleStages() {
        return LOADING_STAGES.filter(stage => {
            if (stage.optional && stage.conditional) {
                return stage.conditional();
            }
            return true;
        });
    }

    // Retry loading
    function retry() {
        console.log(LOG_PREFIX, 'Retrying loading');
        window.location.reload();
    }

    // Clear caches and retry
    function clearCachesAndRetry() {
        console.log(LOG_PREFIX, 'Clearing caches and retrying');
        
        if ('caches' in window) {
            caches.keys().then(cacheNames => {
                return Promise.all(cacheNames.map(key => caches.delete(key)));
            }).then(() => {
                if ('serviceWorker' in navigator) {
                    navigator.serviceWorker.getRegistrations().then(registrations => {
                        return Promise.all(registrations.map(reg => reg.unregister()));
                    });
                }
            }).finally(() => {
                window.location.reload();
            });
        } else {
            window.location.reload();
        }
    }

    // Go offline mode
    function goOffline() {
        console.log(LOG_PREFIX, 'Switching to offline mode');
        
        // Store offline preference
        localStorage.setItem('mystira_offline_mode', 'true');
        
        // Reload to trigger offline experience
        window.location.reload();
    }

    // Report problem (copy diagnostic info)
    function reportProblem() {
        console.log(LOG_PREFIX, 'Reporting problem');
        
        const diagnosticInfo = {
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            url: window.location.href,
            loadingStages: LOADING_STAGES.map(stage => ({
                id: stage.id,
                message: stage.message,
                isOptional: stage.optional
            })),
            currentStage: LOADING_STAGES[currentStageIndex]?.id,
            elapsedTime: Date.now() - startTime,
            progressData: progressData,
            swRegistration: !!window.swRegistration,
            swUpdateAvailable: !!window.swUpdateAvailable,
            connectionType: navigator.connection ? navigator.connection.effectiveType : 'unknown'
        };
        
        // Copy to clipboard
        const diagnosticText = JSON.stringify(diagnosticInfo, null, 2);
        
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(diagnosticText).then(() => {
                alert('Diagnostic information has been copied to your clipboard. Please paste it when reporting the problem.');
            }).catch(() => {
                // Fallback to showing in modal
                showDiagnosticModal(diagnosticText);
            });
        } else {
            showDiagnosticModal(diagnosticText);
        }
    }

    // Show diagnostic info in modal
    function showDiagnosticModal(text) {
        const modalHtml = `
            <div class="modal fade" id="diagnosticModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Diagnostic Information</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>Please copy this information when reporting the problem:</p>
                            <pre style="max-height: 300px; overflow-y: auto;">${text}</pre>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                            <button type="button" class="btn btn-primary" onclick="document.querySelector('#diagnosticModal pre').select(); document.execCommand('copy');">Copy to Clipboard</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal if any
        const existingModal = document.getElementById('diagnosticModal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Add modal to body
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('diagnosticModal'));
        modal.show();
    }

    // Public API
    return {
        initialize: initialize,
        start: () => {
            console.log(LOG_PREFIX, 'Manual start triggered');
            if (loadingInitialized) {
                initialize();
            } else {
                console.warn(LOG_PREFIX, 'Cannot start - not initialized yet, waiting for DOM...');
                // Wait for DOM to be ready then start
                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', () => {
                        loadingInitialized = true;
                        console.log(LOG_PREFIX, 'DOM now ready, starting initialization');
                        initialize();
                    });
                } else {
                    loadingInitialized = true;
                    console.log(LOG_PREFIX, 'DOM already ready, starting initialization');
                    initialize();
                }
            }
        },
        updateStage: updateStage,
        updateProgress: updateProgress,
        complete: complete,
        retry: retry,
        clearCachesAndRetry: clearCachesAndRetry,
        goOffline: goOffline,
        reportProblem: reportProblem,
        
        // Event handlers
        setStageChangeHandler: (handler) => eventHandlers.onStageChange = handler,
        setProgressUpdateHandler: (handler) => eventHandlers.onProgressUpdate = handler,
        setTimeoutWarningHandler: (handler) => eventHandlers.onTimeoutWarning = handler,
        setTimeoutHandler: (handler) => eventHandlers.onTimeout = handler,
        setCompleteHandler: (handler) => eventHandlers.onComplete = handler,
        
        // Getters
        getCurrentStage: () => LOADING_STAGES[currentStageIndex],
        getElapsedTime: () => Date.now() - startTime,
        isLoadingComplete: () => isComplete,
        getProgressData: () => ({ ...progressData })
    };
})();

// Initialize loading tracker when DOM is ready, but don't auto-start
let loadingInitialized = false;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        console.log(LOG_PREFIX, 'DOM loaded, waiting for manual initialization');
        loadingInitialized = true;
    });
} else {
    console.log(LOG_PREFIX, 'DOM already loaded, waiting for manual initialization');
    loadingInitialized = true;
}