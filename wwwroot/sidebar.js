async function getJSON(url) {
    const resp = await fetch(url);
    if (!resp.ok) {
        alert('Could not load tree data. See console for more details.');
        console.error(await resp.text());
        return [];
    }
    return resp.json();
}

function createTreeNode(id, text, icon, children = false) {
    return { id, text, children, itree: { icon } };
}

async function getHubs() {
    const hubs = await getJSON('/api/hubs');
    return hubs.map(hub => createTreeNode(`hub|${hub.id}`, hub.attributes.name, 'icon-hub', true));
}

async function getProjects(hubId) {
    const projects = await getJSON(`/api/hubs/${hubId}/projects`);
    return projects.map(project => createTreeNode(`project|${hubId}|${project.id}`, project.attributes.name, 'icon-project', true));
}

async function getContents(hubId, projectId, folderId = null,sheetSetId=null) {
    var url = '';
    
    if(folderId)
        url = `?folder_id=${folderId}`;
    else if(sheetSetId)
        url = `?set_id=${sheetSetId}`;
     
    const contents = await getJSON(`/api/hubs/${hubId}/projects/${projectId}/contents` + url);
    var nodes =  []; 
    var  folderContents = JSON.parse(contents.folderContents);
    var  sheetsContents = JSON.parse(contents.sheetsContents);

    nodes = folderContents.map(item => {
        if (item.type === 'folders') {
            return createTreeNode(`folder|${hubId}|${projectId}|${item.id}`, item.attributes.displayName, 'icon-my-folder', true);
        } else {
            return createTreeNode(`item|${hubId}|${projectId}|${item.id}`, item.attributes.displayName, 'icon-item', true);
        }
    });

    nodes= nodes.concat(sheetsContents.map(item => {

        if (item.type === 'sheetSet') {
            return createTreeNode(`sheetSet|${hubId}|${projectId}|${item.id}`, item.name, 'icon-my-folder', true);
        } else {
            return createTreeNode(`sheet|${hubId}|${projectId}|${item.viewable.urn}|${item.viewable.guid}`, item.name, 'icon-item', false);
        } 
    }
    ));
    return nodes;
}

async function getVersions(hubId, projectId, itemId) {
    const versions = await getJSON(`/api/hubs/${hubId}/projects/${projectId}/contents/${itemId}/versions`);
    return versions.map(version => createTreeNode(`version|${version.id}`, version.attributes.createTime, 'icon-version'));
}

export function initTree(selector, onSelectionChanged) {
    // See http://inspire-tree.com
    const tree = new InspireTree({
        data: function (node) {
            if (!node || !node.id) {
                return getHubs();
            } else {
                const tokens = node.id.split('|');
                switch (tokens[0]) {
                    case 'hub': return getProjects(tokens[1]);
                    case 'project': return getContents(tokens[1], tokens[2]);
                    case 'folder': return getContents(tokens[1], tokens[2], tokens[3],null);
                    case 'sheetSet': return getContents(tokens[1], tokens[2],null, tokens[3]);
                    case 'item': return getVersions(tokens[1], tokens[2], tokens[3]);
                    default: return [];
                }
            }
        }
    });
    tree.on('node.click', function (event, node) {
        event.preventTreeDefault();
        const tokens = node.id.split('|');
        if (tokens[0] === 'version') {
            onSelectionChanged(tokens[1],null);
        }else if(tokens[0] === 'sheet'){
            onSelectionChanged(tokens[3],tokens[4]);

        }
    });
    return new InspireTreeDOM(tree, { target: selector });
}
