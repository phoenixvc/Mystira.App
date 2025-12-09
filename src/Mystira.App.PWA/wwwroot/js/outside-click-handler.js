// wwwroot/js/outside-click-handler.js

function initializeOutsideClickHandler(dotNetObject, elementId) {
    function onBodyClick(event) {
        const target = document.getElementById(elementId);
        if (target && !target.contains(event.target)) {
            dotNetObject.invokeMethodAsync('HandleOutsideClick');
        }
    }

    document.body.addEventListener('click', onBodyClick);

    return {
        dispose: () => {
            document.body.removeEventListener('click', onBodyClick);
        }
    };
}

window.outsideClickHandler = {
    initialize: initializeOutsideClickHandler
};
