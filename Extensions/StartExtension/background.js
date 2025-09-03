chrome.runtime.onMessage.addListener(async (request, sender, sendResponse) => {
    if (request.action === "fetchJiraStatus") {
        const { issueKey } = request;
    // TODO: Securely provide Jira credentials
    const username = '';
    const apiToken = '';

        try {
            const response = await fetch(`https://your-jira-url/rest/api/2/issue/${issueKey}`, {
                headers: {
                    'Authorization': 'Basic ' + btoa(`${username}:${apiToken}`)
                }
            });
            const data = await response.json();
            sendResponse({ status: data.fields.status.name });
        } catch (error) {
            console.error('Failed to fetch Jira status:', error);
            sendResponse({ error: error.message });
        }
    }
    // This line keeps the message channel open for asynchronous response
    return true;
});
