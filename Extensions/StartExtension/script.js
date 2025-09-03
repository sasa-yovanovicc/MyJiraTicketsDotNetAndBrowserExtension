document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-issue]').forEach(element => {
        const issueKey = element.getAttribute('data-issue');
        fetchJiraStatus(issueKey, element);
    });
});

function fetchJiraStatus(issueKey, element) {
    chrome.runtime.sendMessage({ action: "fetchJiraStatus", issueKey }, (response) => {
        if (chrome.runtime.lastError) {
            console.error('Error connecting to background script:', chrome.runtime.lastError.message);
            element.querySelector('.status').textContent = "Error loading status";
            return;
        }

        if (response && response.status) {
            const statusSpan = element.querySelector('.status');
            statusSpan.textContent = `Status: ${response.status}`;
            statusSpan.style.color = "green";
        } else {
            element.querySelector('.status').textContent = "Error loading status";
            console.error('Failed to fetch Jira status:', response.error);
        }
    });
}
